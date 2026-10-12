using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Vertex;
using XenoKit.Engine.Vfx.Particle;
using XenoKit.Helper.Find;
using Xv2CoreLib.EMP_NEW;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Rendering
{
    public class ParticleBatch : RenderObject
    {
        private ParticleBatchItem[] BatchItems;
        private VertexPositionTextureColor[] Vertices;

        private int batchIndex = 0; //The next index to add a batch to... and the number of items batched

        private bool _tryDownsize = false;
        private int _batchNumAtLastSlowUpdate = 0;
        private int _maxBatchNumSinceLastSlowUpdate = 0;
        private const int MinBatchItemCount = 64;

        // A file-defined material can reference a shader whose vertex inputs don't match VertexPositionTextureColor.
        // D3D then fails to build the input layout and throws when the particles are drawn. Materials that fail once
        // are tracked here and skipped, so one bad material can't take down the render loop.
        private static readonly HashSet<Xv2ShaderEffect> incompatibleMaterials = new HashSet<Xv2ShaderEffect>();
        private static readonly BlendState subtractiveBlendState = CreateSubtractiveBlendState();

        public readonly EMP_File EmpFile;
        public readonly uint RenderGroupKey;
        public readonly bool NoGlare;

        // Members are in file order. The whole group draws with the material and textures of the first member that has live particles.
        private ParticleNode[] Members;
        private ParticleEmissionData[] MemberData;
        private int firstLiveMember = -1;

        public ParticleEmissionData EmissionData => MemberData[firstLiveMember >= 0 ? firstLiveMember : 0];

        public int NumBatch { get; private set; }
        public int MaxBatchSinceLastSlowUpdate => _maxBatchNumSinceLastSlowUpdate;

        public override int LowRezMode
        {
            get
            {
                if (EmissionData?.Material == null) return 0;
                if (ParticleEmissionBase.UsesSubtractiveMaterial(EmissionData.Material)) return 0;
                if (EmissionData.Material.MatParam.LowRez == 1) return 1;
                if (EmissionData.Material.MatParam.LowRezSmoke == 1) return 2;
                return 0;
            }
        }

        public ParticleBatch(EMP_File empFile, uint renderGroupKey, bool noGlare, ParticleNode particleNode)
        {
            EmpFile = empFile;
            RenderGroupKey = renderGroupKey;
            NoGlare = noGlare;
            BatchItems = new ParticleBatchItem[MinBatchItemCount];
            Vertices = new VertexPositionTextureColor[MinBatchItemCount * 6];
            LoadMembers();

            if (!HasMember(particleNode))
                throw new InvalidOperationException($"ParticleBatch: node \"{particleNode.Name}\" is not in render group {renderGroupKey} of its EMP file.");
        }

        public bool HasMember(ParticleNode node) => Array.IndexOf(Members, node) >= 0;

        public void LoadMembers()
        {
            Members = EmpFile.GetAllParticleNodes()
                .Where(node => node.IsEmission && node.EmissionNode.Texture.RenderGroupKey == RenderGroupKey)
                .ToArray();
            MemberData = Members.Select(node => CompiledObjectManager.GetCompiledObject<ParticleEmissionData>(node)).ToArray();
            firstLiveMember = -1;
        }

        public void MarkLive(ParticleNode node)
        {
            int memberIndex = Array.IndexOf(Members, node);

            if (memberIndex >= 0 && (firstLiveMember < 0 || memberIndex < firstLiveMember))
                firstLiveMember = memberIndex;
        }

        private void ExpandIfNeeded(int targetSize)
        {
            if(targetSize >= BatchItems.Length)
            {
                int oldSize = BatchItems.Length;
                int newSize = oldSize + oldSize / 2; // grow by x1.5
                newSize = (newSize + 63) & (~63); // grow in chunks of 64.
                Array.Resize(ref BatchItems, newSize);
                Array.Resize(ref Vertices, newSize * 6);
            }
        }

        private void DownsizeIfNeeded(int targetSize)
        {
            if (targetSize < BatchItems.Length && BatchItems.Length > MinBatchItemCount)
            {
                int newSize = MathHelper.Clamp(((targetSize + 64 - 1) / 64) * 64, MathHelper.Max(64, NumBatch), int.MaxValue);

                if(newSize != BatchItems.Length)
                {
                    Array.Resize(ref BatchItems, newSize);
                    Array.Resize(ref Vertices, newSize * 6);
                }
            }
        }

        public void AddToBatch(ParticleBatchItem batch)
        {
            ExpandIfNeeded(batchIndex);
            BatchItems[batchIndex++] = batch;
        }

        public void SlowUpdate()
        {
            _tryDownsize = true;
        }

        private void TryDownsize()
        {
            if (_maxBatchNumSinceLastSlowUpdate < BatchItems.Length && NumBatch < _maxBatchNumSinceLastSlowUpdate)
            {
                //Resize to the max amount of ParticleBatchItems used since the last SlowUpdate
                DownsizeIfNeeded(_maxBatchNumSinceLastSlowUpdate + 1);
            }

            _batchNumAtLastSlowUpdate = NumBatch;
            _maxBatchNumSinceLastSlowUpdate = 0;
            _tryDownsize = false;
        }

        #region Draw

        public override void Update()
        {
            for (int i = 0; i < MemberData.Length; i++)
            {
                MemberData[i].Update();
            }
        }

        public override void Draw()
        {
            NumBatch = batchIndex;

            if (_tryDownsize)
            {
                TryDownsize();
            }
            else if (batchIndex > _maxBatchNumSinceLastSlowUpdate)
            {
                _maxBatchNumSinceLastSlowUpdate = batchIndex;
            }

            ParticleEmissionData data = EmissionData;
            firstLiveMember = -1;

            if (batchIndex == 0) return;

            if (!RenderSystem.CheckDrawPass(data.Material)) return;

            if (incompatibleMaterials.Contains(data.Material))
            {
                batchIndex = 0;
                return;
            }

            UpdateVertices();

            RenderSystem.MeshDrawCalls++;

            //Set samplers/textures
            for (int i = 0; i < data.Samplers.Length; i++)
            {
                GraphicsDevice.SamplerStates[data.Samplers[i].samplerSlot] = data.Samplers[i].state;

                GraphicsDevice.Textures[data.Samplers[i].textureSlot] = Xv2Texture.GetTextureOrDefault(data.Textures[i]);
            }

            data.Material.World = Matrix4x4.Identity;

            //Shader passes and vertex drawing
            foreach (EffectPass pass in data.Material.CurrentTechnique.Passes)
            {
                data.Material.SetGlareOutputAllowed(!NoGlare);
                pass.Apply();

                if (data.Material.MatParam.AlphaBlendType == 2)
                {
                    GraphicsDevice.BlendState = subtractiveBlendState;
                }

                try
                {
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, batchIndex * 2);
                }
                catch (Exception ex)
                {
                    incompatibleMaterials.Add(data.Material);
                    Log.Add($"ParticleBatch: skipped drawing particles because material '{data.Material.Material?.Name}' is not compatible with the particle vertex format. {ex.Message}", LogType.Warning);
                    break;
                }
            }

            batchIndex = 0;
        }

        private static BlendState CreateSubtractiveBlendState()
        {
            BlendState blendState = new BlendState { IndependentBlendEnable = true };
            blendState.ApplySubtractive(0);
            blendState.ApplySubtractive(1);
            return blendState;
        }

        private void UpdateVertices()
        {
            const int VERTEX_TOP_LEFT = 0;
            const int VERTEX_TOP_RIGHT = 1;
            const int VERTEX_BOTTOM_LEFT_ALT = 2;
            const int VERTEX_BOTTOM_LEFT = 3;
            const int VERTEX_TOP_RIGHT_ALT = 4;
            const int VERTEX_BOTTOM_RIGHT = 5;

            for (int i = 0; i < batchIndex; i++)
            {
                int vertIdx = i * 6;

                //Position
                Vertices[vertIdx + VERTEX_TOP_LEFT].Position.X = -BatchItems[i].ScaleU_First;
                Vertices[vertIdx + VERTEX_TOP_LEFT].Position.Y = BatchItems[i].ScaleV;
                Vertices[vertIdx + VERTEX_TOP_LEFT].Position.Z = 0f;
                Vertices[vertIdx + VERTEX_TOP_RIGHT].Position.X = BatchItems[i].ScaleU_First;
                Vertices[vertIdx + VERTEX_TOP_RIGHT].Position.Y = BatchItems[i].ScaleV;
                Vertices[vertIdx + VERTEX_TOP_RIGHT].Position.Z = 0f;
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position.X = -BatchItems[i].ScaleU;
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position.Y = -BatchItems[i].ScaleV;
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position.Z = 0f;
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position.X = BatchItems[i].ScaleU;
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position.Y = -BatchItems[i].ScaleV;
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position.Z = 0f;

                //UV
                ParticleUvMap primaryMap = BatchItems[i].UV.GetMapping();
                Vertices[vertIdx + VERTEX_TOP_LEFT].TextureUV = primaryMap.Map(Vector2.Zero);
                Vertices[vertIdx + VERTEX_TOP_RIGHT].TextureUV = primaryMap.Map(Vector2.UnitX);
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].TextureUV = primaryMap.Map(Vector2.UnitY);
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].TextureUV = primaryMap.Map(Vector2.One);

                ParticleUvMap secondaryMap = BatchItems[i].UV1.GetMapping();
                Vertices[vertIdx + VERTEX_TOP_LEFT].TextureUV1 = secondaryMap.Map(Vector2.Zero);
                Vertices[vertIdx + VERTEX_TOP_RIGHT].TextureUV1 = secondaryMap.Map(Vector2.UnitX);
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].TextureUV1 = secondaryMap.Map(Vector2.UnitY);
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].TextureUV1 = secondaryMap.Map(Vector2.One);

                //Color
                Vertices[vertIdx + VERTEX_TOP_LEFT].SetColor(BatchItems[i].TopColor);
                Vertices[vertIdx + VERTEX_TOP_RIGHT].SetColor(BatchItems[i].TopColor);
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].SetColor(BatchItems[i].BottomColor);
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].SetColor(BatchItems[i].BottomColor);

                //Transform vertices into world space
                Vertices[vertIdx + VERTEX_TOP_LEFT].Position = Vector3.Transform(Vertices[vertIdx + VERTEX_TOP_LEFT].Position, BatchItems[i].World);
                Vertices[vertIdx + VERTEX_TOP_RIGHT].Position = Vector3.Transform(Vertices[vertIdx + VERTEX_TOP_RIGHT].Position, BatchItems[i].World);
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position = Vector3.Transform(Vertices[vertIdx + VERTEX_BOTTOM_LEFT].Position, BatchItems[i].World);
                Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position = Vector3.Transform(Vertices[vertIdx + VERTEX_BOTTOM_RIGHT].Position, BatchItems[i].World);

                //Duplicate vertices
                Vertices[vertIdx + VERTEX_BOTTOM_LEFT_ALT] = Vertices[vertIdx + VERTEX_BOTTOM_LEFT];
                Vertices[vertIdx + VERTEX_TOP_RIGHT_ALT] = Vertices[vertIdx + VERTEX_TOP_RIGHT];
            }
        }

        #endregion

    }

    public struct ParticleBatchItem
    {
        //Total size = 92 bytes

        public ParticleUV UV;
        public ParticleUV UV1;
        public float ScaleU_First;
        public float ScaleU;
        public float ScaleV;
        public Color TopColor;
        public Color BottomColor;
        public Matrix4x4 World;
    }
}
