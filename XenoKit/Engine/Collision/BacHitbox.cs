using Microsoft.Xna.Framework;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BAC;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;
using SimdQuaternion = System.Numerics.Quaternion;
using Xv2CoreLib.Resource;

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
        private SimdVector3 PreviousTranslation;

        public int Team;

        public readonly BacEntryInstance BacEntry;
        public readonly Actor OwnerActor;
        public readonly Actor SpawnActor;
        public readonly BAC_Type1 Hitbox;
        public BoundingBox BoundingBox;
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
            Matrix4x4 world = WorldMatrix;

            if (world.Translation == PreviousTranslation) return; //No need to update

            if (Hitbox.BoundingBoxType == BAC_Type1.BoundingBoxTypeEnum.MinMax)
            {
                BoundingBox = new BoundingBox((new Vector3(Hitbox.MinX, Hitbox.MinY, Hitbox.MinZ) / 2) + HitboxPosition + world.Translation, (new Vector3(Hitbox.MaxX, Hitbox.MaxY, Hitbox.MaxZ) / 2) + HitboxPosition + world.Translation);
            }
            else
            {
                BoundingBox = new BoundingBox(new Vector3(-(Hitbox.Size / 2)) + HitboxPosition + world.Translation, new Vector3((Hitbox.Size / 2)) + HitboxPosition + world.Translation);
            }

            PreviousTranslation = world.Translation;
        }

        public bool IsContextValid()
        {
            if (!BacEntry.InScope || BacEntry.IsFinished) return false;
            return BacEntry.IsValidTime(Hitbox.StartTime, Hitbox.Duration);
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
