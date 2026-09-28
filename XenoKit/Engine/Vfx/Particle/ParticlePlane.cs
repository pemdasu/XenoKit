using Microsoft.Xna.Framework;
using System;
using XenoKit.Engine.Rendering;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticlePlane : ParticleEmissionBase
    {
        protected BoundingBox AABB = new BoundingBox();

        private ParticleBatch Batch;

        public override void Initialize(Matrix4x4 emitPoint, SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            base.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            Batch = RenderSystem.ParticleBatcher.GetBatch(node, effectPart.NoGlare);
        }

        public override void Release()
        {
            ObjectPoolManager.ParticlePlanePool.ReleaseObject(this);
        }

        private void UpdateAABB()
        {
            float scaleU_FirstVertex = ((Node.NodeFlags & NodeFlags1.EnableScaleXY) != 0) ? ScaleBase : ScaleU;

            float aabbX = Math.Max(Math.Abs(scaleU_FirstVertex), Math.Abs(ScaleU));
            float aabbY = Math.Abs(ScaleV);
            Vector3 min = new Vector3(-aabbX, -aabbY, 0f);
            Vector3 max = new Vector3(aabbX, aabbY, 0f);

            AABB = new BoundingBox(min, max);
        }

        public override void Update()
        {
            DrawThisFrame = true;
            EmissionData.Update();
            ParticleUV.Update(ParticleSystem.CurrentFrameDelta);

            if (Node.EmissionNode.VelocityOriented && Node.EmissionNode.BillboardType == ParticleBillboardType.Camera)
            {
                VelocityOrientedAdjustment = Matrix4x4.CreateTranslation(new SimdVector3(0, (ScaleV + ScaleV_Variance) / 2f, 0));
            }
            else
            {
                VelocityOrientedAdjustment = Matrix4x4.Identity;
            }

            StartUpdate();

            if (State == NodeState.Active)
            {
                UpdateRotation();
                UpdateScale();
                UpdateColor();
                UpdateAABB();

                if (Node.EmissionNode.BillboardType == ParticleBillboardType.Camera &&
                    Node.EmissionNode.VelocityOriented && Velocity == Vector3.Zero)
                    DrawThisFrame = false;

                AbsoluteTransform = ParticleBillboard.CreateWorld(this, Node.EmissionNode.BillboardType,
                    Node.EmissionNode.VelocityOriented, RotationAmount, RandomDirection, Node.EmissionNode.Texture.RenderDepth);
            }

            UpdateChildrenNodes();
            EndUpdate();
            DrawBatch();
        }

        public void DrawBatch()
        {
            if (!Viewport.Instance.DrawThisFrame || !DrawThisFrame) return;
            if (EmissionData == null || ParticleSystem == null) return;
            //if (!ParticleSystem.DrawThisFrame) return;
            //if (!RenderSystem.CheckDrawPass(EmissionData.Material) || !ParticleSystem.DrawThisFrame) return;

            if (!FrustumIntersects(AbsoluteTransform, AABB))
                return;

            //RenderSystem.MeshDrawCalls++;

            if (State == NodeState.Active && (Node.NodeFlags & NodeFlags1.Hide) != NodeFlags1.Hide)
            {
                if (Batch.IsDestroyed)
                {
                    Batch = RenderSystem.ParticleBatcher.GetBatch(Node, SourceEffectPart.NoGlare);
                }

                Batch.AddToBatch(CreateBatchItem());
                /*
                //Set samplers/textures
                for (int i = 0; i < EmissionData.Samplers.Length; i++)
                {
                    GraphicsDevice.SamplerStates[EmissionData.Samplers[i].samplerSlot] = EmissionData.Samplers[i].state;
                    GraphicsDevice.VertexSamplerStates[EmissionData.Samplers[i].samplerSlot] = EmissionData.Samplers[i].state;

                    if (EmissionData.Textures[i] != null)
                    {
                        GraphicsDevice.VertexTextures[EmissionData.Samplers[i].textureSlot] = EmissionData.Textures[i].Texture;
                        GraphicsDevice.Textures[EmissionData.Samplers[i].textureSlot] = EmissionData.Textures[i].Texture;
                    }
                }

                EmissionData.Material.World = AbsoluteTransform;

                //Shader passes and vertex drawing
                foreach (EffectPass pass in EmissionData.Material.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, 2);
                }
                */
            }

        }

        private ParticleBatchItem CreateBatchItem()
        {
            //Special case for when Scale XY is used. The first vertex still uses Scale Base for U, but not V (game bug? seems weird...)
            float scaleU_FirstVertex = ((Node.NodeFlags & NodeFlags1.EnableScaleXY) != 0) ? ScaleBase : ScaleU;
            bool useBottomColor = (Node.NodeFlags & NodeFlags1.EnableSecondaryColor) != 0 && (Node.NodeFlags & NodeFlags1.FlashOnGen) == 0;
            
            return new ParticleBatchItem()
            {
                UV = ParticleUV,
                World = AbsoluteTransform,
                ScaleU = ScaleU,
                ScaleV = ScaleV,
                ScaleU_First = scaleU_FirstVertex,
                TopColor = new Color(PrimaryColor[0], PrimaryColor[1], PrimaryColor[2], PrimaryColor[3]),
                BottomColor = useBottomColor ? new Color(SecondaryColor[0], SecondaryColor[1], SecondaryColor[2], SecondaryColor[3]) : new Color(PrimaryColor[0], PrimaryColor[1], PrimaryColor[2], PrimaryColor[3])
            };
        }

    }
}
