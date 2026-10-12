using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Collision;
using Xv2CoreLib.BCS;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Animation
{
    public sealed class BcsColliderPreview : IDisposable
    {
        private Actor actor;
        private SkeletonData data;
        private bool dirty;
        private readonly List<Collider> colliders = new List<Collider>();

        public void Invalidate() => dirty = true;

        public void Draw(Actor currentActor, SkeletonData currentData, Bone selectedBone)
        {
            if (dirty || actor != currentActor || data != currentData)
            {
                Dispose();
                actor = currentActor;
                data = currentData;
                dirty = false;
                if (actor?.Skeleton == null || data?.Bones == null) return;
                foreach (Bone bone in data.Bones)
                {
                    int index = actor.Skeleton.GetBoneIndex(bone.BoneName);
                    if (index >= 0) colliders.Add(new Collider(bone, index));
                }
            }
            if (actor?.Skeleton == null) return;
            foreach (Collider collider in colliders)
            {
                Matrix4x4 boneWorld = actor.Skeleton.Bones[collider.BoneIndex].AbsoluteAnimationMatrix * actor.Transform;
                collider.Draw(boneWorld, collider.Bone == selectedBone);
            }
        }

        public void Dispose()
        {
            foreach (Collider collider in colliders) collider.Dispose();
            colliders.Clear();
            actor = null;
            data = null;
        }

        private sealed class Collider : IDisposable
        {
            public Bone Bone { get; }
            public int BoneIndex { get; }
            private HitboxVisual visual;
            private Vector3 scale;

            public Collider(Bone bone, int boneIndex)
            {
                Bone = bone;
                BoneIndex = boneIndex;
            }

            public void Draw(Matrix4x4 boneWorld, bool selected)
            {
                Vector3 currentScale = new Vector3(
                    new Vector3(boneWorld.M11, boneWorld.M12, boneWorld.M13).Length(),
                    new Vector3(boneWorld.M21, boneWorld.M22, boneWorld.M23).Length(),
                    new Vector3(boneWorld.M31, boneWorld.M32, boneWorld.M33).Length());
                if (visual == null || Vector3.DistanceSquared(scale, currentScale) > 0.000001f)
                {
                    var shape = ScdPhysics.CreateBcsColliderShape(Bone, boneWorld);
                    if (shape == null) return;
                    if (visual == null) visual = new HitboxVisual(Color.Transparent, Color.Yellow);
                    visual.SetShape(shape);
                    scale = currentScale;
                }
                visual.WireColor = selected ? Color.Cyan : Color.Yellow;
                visual.Draw(ScdPhysics.GetBcsColliderWorld(Bone, boneWorld));
            }

            public void Dispose() => visual?.Dispose();
        }
    }
}
