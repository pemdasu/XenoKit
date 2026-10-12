using Microsoft.Xna.Framework;
using System;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BAC;
using Xv2CoreLib.CBS;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Collision
{
    public class BacHitbox
    {
        public Matrix4x4 WorldMatrix
        {
            get
            {
                if (Hitbox == null) return Matrix4x4.Identity;
                Matrix4x4 world = Matrix4x4.Identity;

                if (boneIdx != -1 && SpawnActor != null)
                {
                    world = SpawnActor.GetAbsoluteBoneMatrix(boneIdx);

                    //Hitbox doesn't rotate with b_C_Base, so the rotation needs to be removed
                    if (isBaseBone)
                    {
                        world = Matrix4x4.CreateTranslation(world.Translation);
                    }
                }

                return world;
            }
        }
        public int Team;

        public readonly BacEntryInstance BacEntry;
        public readonly Actor OwnerActor;
        public readonly Actor SpawnActor;
        public readonly BAC_Type1 Hitbox;
        public BoundingBox BoundingBox;
        public bool IsSupported { get; private set; }
        private bool hasHit = false;
        private HitShapeType shapeType = HitShapeType.None;
        private SimdVector3 shapeCenter;
        private SimdVector3 capsuleStart;
        private SimdVector3 capsuleEnd;
        private float shapeRadius;
        private SimdVector3 boxHalfExtents;
        private Matrix4x4 boxWorld;
        private int boneIdx = -1;
        private bool isBaseBone = false;

        private SimdVector3 HitboxPosition;

        public BacHitbox(BacEntryInstance bacEntry, BAC_Type1 bacHitbox, Actor spawnOnActor, Actor owner, int team)
        {
            Team = team;
            BacEntry = bacEntry;
            Hitbox = bacHitbox;
            BoundingBox = new BoundingBox();
            SpawnActor = spawnOnActor;
            OwnerActor = owner;

            string boneName = Hitbox.BoneLink.ToString();
            isBaseBone = boneName == Xv2CoreLib.ESK.ESK_File.BaseBone;

            if (SpawnActor != null)
            {
                boneIdx = SpawnActor.Skeleton.GetBoneIndex(boneName);
            }
            else
            {
                Log.Add($"Hitbox tried spawning on actor, but no actor was found in the scene!", LogType.Warning);
            }

            HitboxPosition = new SimdVector3(Hitbox.PositionX, Hitbox.PositionY, Hitbox.PositionZ);
        }

        public void UpdateHitbox()
        {
            IsSupported = false;
            if (SpawnActor == null)
                return;

            float cbsScaling = GetSizeScale(SpawnActor, Hitbox);

            if (!IsFinite(cbsScaling))
                return;

            HitboxPosition = new SimdVector3(Hitbox.PositionX, Hitbox.PositionY, Hitbox.PositionZ) * cbsScaling;
            Matrix4x4 world = WorldMatrix;
            SimdVector3 center = SimdVector3.Transform(HitboxPosition, world);

            if (!IsFinite(center))
                return;

            shapeType = HitShapes.GetShapeType((int)Hitbox.BoundingBoxType);
            shapeCenter = center;
            shapeRadius = GetRadius(cbsScaling);

            switch (shapeType)
            {
                case HitShapeType.Sphere:
                    SetBoundsFromCenter(center, new SimdVector3(shapeRadius));
                    break;
                case HitShapeType.Capsule:
                    SimdVector3 endpointA = SimdVector3.Transform(
                        HitboxPosition + new SimdVector3(Hitbox.MinX, Hitbox.MinY, Hitbox.MinZ) * cbsScaling,
                        world);
                    SimdVector3 endpointB = SimdVector3.Transform(
                        HitboxPosition + new SimdVector3(Hitbox.MaxX, Hitbox.MaxY, Hitbox.MaxZ) * cbsScaling,
                        world);

                    if (!IsFinite(endpointA) || !IsFinite(endpointB) || !IsFinite(shapeRadius) || shapeRadius <= 0f)
                        return;

                    capsuleStart = endpointA;
                    capsuleEnd = endpointB;
                    SetBoundsFromMinMax(
                        SimdVector3.Min(endpointA, endpointB) - new SimdVector3(shapeRadius),
                        SimdVector3.Max(endpointA, endpointB) + new SimdVector3(shapeRadius));
                    break;
                case HitShapeType.Box:
                    boxHalfExtents = new SimdVector3(
                        Math.Abs(Hitbox.Size),
                        Math.Abs(Hitbox.MinX),
                        Math.Abs(Hitbox.MinY)) * Math.Abs(cbsScaling);
                    boxWorld = GetBoxWorld(Hitbox, world, SpawnActor.Transform, cbsScaling);
                    SimdVector3 worldHalfExtents = new SimdVector3(
                        Math.Abs(boxWorld.M11) * boxHalfExtents.X + Math.Abs(boxWorld.M21) * boxHalfExtents.Y + Math.Abs(boxWorld.M31) * boxHalfExtents.Z,
                        Math.Abs(boxWorld.M12) * boxHalfExtents.X + Math.Abs(boxWorld.M22) * boxHalfExtents.Y + Math.Abs(boxWorld.M32) * boxHalfExtents.Z,
                        Math.Abs(boxWorld.M13) * boxHalfExtents.X + Math.Abs(boxWorld.M23) * boxHalfExtents.Y + Math.Abs(boxWorld.M33) * boxHalfExtents.Z);
                    SetBoundsFromCenter(center, worldHalfExtents);
                    break;
                default:
                    return;
            }
        }

        //Shapes 0-2 scale the hitbox position and size by CBS F_04. The game uses F_08 or F_12 under actor states that are not known yet.
        public static float GetSizeScale(Actor actor, BAC_Type1 hitbox)
        {
            if (!HitShapes.UsesSizeScale((int)hitbox.BoundingBoxType))
                return 1f;

            int bodyId = actor.Skeleton.GetActiveBoneScaleId();
            CBS_Entry cbsEntry = actor.CharacterData.CbsEntry?.Find(entry => entry.BodyId == bodyId);
            return cbsEntry != null ? cbsEntry.F_04 : 1f;
        }

        private float GetRadius(float cbsScaling)
        {
            return Math.Abs(Hitbox.Size * cbsScaling);
        }

        public static Matrix4x4 GetBoxWorld(BAC_Type1 hitbox, Matrix4x4 boneWorld, Matrix4x4 actorWorld, float sizeScale)
        {
            Matrix4x4 world = Matrix4x4.CreateRotationX(hitbox.MinZ) * Matrix4x4.CreateRotationY(hitbox.MaxX) * Matrix4x4.CreateRotationZ(hitbox.MaxY) * actorWorld;
            world.Translation = SimdVector3.Transform(new SimdVector3(hitbox.PositionX, hitbox.PositionY, hitbox.PositionZ) * sizeScale, boneWorld);
            return world;
        }

        private void SetBoundsFromCenter(SimdVector3 center, SimdVector3 halfExtents)
        {
            if (!IsFinite(halfExtents))
                return;

            BoundingBox = new BoundingBox(
                Extensions.ToXna(center - halfExtents),
                Extensions.ToXna(center + halfExtents));
            IsSupported = true;
        }

        private void SetBoundsFromMinMax(SimdVector3 min, SimdVector3 max)
        {
            if (!IsFinite(min) || !IsFinite(max))
                return;

            BoundingBox = new BoundingBox(Extensions.ToXna(min), Extensions.ToXna(max));
            IsSupported = true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(SimdVector3 value)
        {
            return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
        }

        public bool IsContextValid()
        {
            if (!BacEntry.InScope || BacEntry.IsFinished) return false;
            return BacEntry.IsValidTime(Hitbox.StartTime, Hitbox.Duration);
        }

        //0x1000 only picks a collision layer and hits like 0. 0x8000 only touches records with the same owner, so it never hits another actor.
        public bool CanHit()
        {
            BAC_Type1.HitboxFlagsEnum impactType = Hitbox.GetImpactType();

            if ((impactType & BAC_Type1.HitboxFlagsEnum.ImpactType_Unk4) != 0)
                return false;

            if ((impactType & BAC_Type1.HitboxFlagsEnum.ImpactType_Single) != 0)
                return !hasHit;

            return true;
        }

        //A single impact hitbox stops after its first hit on any actor.
        public void RecordHit()
        {
            hasHit = true;
        }

        public bool IntersectsSphere(BoundingSphere sphere)
        {
            SimdVector3 sphereCenter = sphere.Center.ToNumerics();

            switch (shapeType)
            {
                case HitShapeType.Sphere:
                    return HitShapes.SphereIntersectsSphere(shapeCenter, shapeRadius, sphereCenter, sphere.Radius);
                case HitShapeType.Capsule:
                    return HitShapes.CapsuleIntersectsSphere(capsuleStart, capsuleEnd, shapeRadius, sphereCenter, sphere.Radius);
                case HitShapeType.Box:
                    return HitShapes.BoxIntersectsSphere(boxWorld, boxHalfExtents, sphereCenter, sphere.Radius);
                default:
                    return false;
            }
        }

        //Flag 0x80 spawns the hitbox on the target. Every other spawn source uses the user's bones.
        public static Actor GetSpawnActor(BAC_Type1 hitbox, Actor user)
        {
            return (hitbox.HitboxFlags & BAC_Type1.HitboxFlagsEnum.SpawnSource_Target) != 0 ? (user?.Opponent ?? user) : user;
        }

        public SimdVector3 GetRelativeDirection(Matrix4x4 matrix)
        {
            SimdVector3 relativeDir = (MathHelpers.Invert(matrix) * WorldMatrix).Translation;
            relativeDir = SimdVector3.Normalize(relativeDir);
            return relativeDir;
        }

        public Matrix4x4 GetAbsoluteHitboxMatrix()
        {
            return WorldMatrix * Matrix4x4.CreateTranslation(HitboxPosition);
        }
    }
}
