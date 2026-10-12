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
            Batch = RenderSystem.ParticleBatcher.GetBatch(system, node, effectPart.NoGlare);
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

            StartUpdate();

            if (State == NodeState.Active)
            {
                UpdateUVs();
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

        public float GetHeight() => ScaleV;

        public void DrawBatch()
        {
            if (!Viewport.Instance.DrawThisFrame) return;
            if (EmissionData == null || ParticleSystem == null || State != NodeState.Active) return;

            if (Batch.IsDestroyed)
            {
                Batch = RenderSystem.ParticleBatcher.GetBatch(ParticleSystem, Node, SourceEffectPart.NoGlare);
            }

            //Live particles pick the group material even when they are culled or hidden.
            Batch.MarkLive(Node);

            if (!DrawThisFrame || !ParticleSystem.DrawThisFrame || (Node.NodeFlags & NodeFlags1.Hide) == NodeFlags1.Hide) return;
            if (!FrustumIntersects(AbsoluteTransform, AABB)) return;

            Batch.AddToBatch(CreateBatchItem());
        }

        private ParticleBatchItem CreateBatchItem()
        {
            //Special case for when Scale XY is used. The first vertex still uses Scale Base for U, but not V (game bug? seems weird...)
            float scaleU_FirstVertex = ((Node.NodeFlags & NodeFlags1.EnableScaleXY) != 0) ? ScaleBase : ScaleU;
            bool useBottomColor = (Node.NodeFlags & NodeFlags1.EnableSecondaryColor) != 0 && (Node.NodeFlags & NodeFlags1.FlashOnGen) == 0;
            
            return new ParticleBatchItem()
            {
                UV = ParticleUV,
                UV1 = TextureUV1,
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
