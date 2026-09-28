using Microsoft.Xna.Framework;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BAC;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    public partial class ProjectileInstance
    {
        internal static Actor GetSpawnActor(Actor user, BAC_Type9 projectileType)
        {
            if (projectileType?.SpawnSource == 1 && SceneManager.Actors[1] != null)
                return SceneManager.Actors[1];

            return user;
        }

        private static Matrix4x4 CreateSpawnTransform(BacEntryInstance bacInstance, BAC_Type9 projectileType)
        {
            Actor spawnActor = GetSpawnActor(bacInstance?.User, projectileType);
            return CreateProjectileWorldTransform(spawnActor, projectileType);
        }

        internal static Matrix4x4 CreateProjectileWorldTransform(Actor spawnActor, BAC_Type9 projectileType)
        {
            Matrix4x4 attachTransform = GetProjectileAttachTransform(spawnActor, projectileType);
            Matrix4x4 parentTransform = CreateProjectileParentTransform(spawnActor, projectileType, attachTransform);
            Matrix4x4 worldTransform = CreateProjectileLocalTransform(projectileType) * parentTransform;
            return ApplyUserDirection1(worldTransform, projectileType);
        }

        internal static Matrix4x4 CreateProjectileLocalTransform(BAC_Type9 projectileType)
        {
            Matrix4x4 rotation = CreateProjectileRotation(projectileType);
            Matrix4x4 position = Matrix4x4.CreateTranslation(new SimdVector3(projectileType.PositionX, projectileType.PositionY, projectileType.PositionZ));

            return rotation * position;
        }

        internal static Matrix4x4 CreateProjectileRotation(BAC_Type9 projectileType)
        {
            if (projectileType == null)
                return Matrix4x4.Identity;

            if (IsUserDirection1SpawnOrientation(projectileType))
                return Matrix4x4.Identity;

            if (IsUserDirection3SpawnOrientation(projectileType))
                return Matrix4x4.CreateFromYawPitchRoll(
                    MathHelper.ToRadians(projectileType.RotationY),
                    MathHelper.ToRadians(projectileType.RotationX),
                    MathHelper.ToRadians(projectileType.RotationZ));

            Matrix4x4 nonYRotation = Matrix4x4.CreateFromYawPitchRoll(
                MathHelper.ToRadians(projectileType.RotationX),
                0f,
                MathHelper.ToRadians(projectileType.RotationZ));
            Matrix4x4 yRotation = Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-projectileType.RotationY));

            return nonYRotation * yRotation;
        }

        internal static Matrix4x4 CreateProjectileParentTransform(Actor spawnActor, BAC_Type9 projectileType, Matrix4x4 attachTransform)
        {
            if (IsUserDirection3SpawnOrientation(projectileType) && spawnActor != null)
            {
                Matrix4x4 parentTransform = GetRotationOnly(spawnActor.Transform);
                parentTransform.Translation = attachTransform.Translation;
                return parentTransform;
            }

            if (IsUserDirection1SpawnOrientation(projectileType))
                return GetUserDirectionParentTransform(spawnActor, attachTransform);

            if (projectileType.SpawnOrientation != SpawnOrientationDefault)
                Log.Add($"Unsupported BSA projectile spawn orientation {projectileType.SpawnOrientation}. Using default orientation.", LogType.Warning);

            return attachTransform;
        }

        private static bool ShouldFollowLiveAttachTransform(BAC_Type9 projectileType)
        {
            return projectileType != null &&
                   (IsDefaultSpawnOrientation(projectileType) ||
                    IsUserDirectionSpawnOrientation(projectileType));
        }

        private static bool IsDefaultSpawnOrientation(BAC_Type9 projectileType)
        {
            return projectileType != null && projectileType.SpawnOrientation == SpawnOrientationDefault;
        }

        private static bool IsUserDirectionSpawnOrientation(BAC_Type9 projectileType)
        {
            return IsUserDirection1SpawnOrientation(projectileType) ||
                   IsUserDirection3SpawnOrientation(projectileType);
        }

        private static bool IsUserDirection1SpawnOrientation(BAC_Type9 projectileType)
        {
            return projectileType != null &&
                   projectileType.SpawnOrientation == SpawnOrientationUserDirection1;
        }

        private static bool IsUserDirection3SpawnOrientation(BAC_Type9 projectileType)
        {
            return projectileType != null &&
                   projectileType.SpawnOrientation == SpawnOrientationUserDirectionValue;
        }

        private static Matrix4x4 GetProjectileAttachTransform(Actor spawnActor, BAC_Type9 projectileType)
        {
            Matrix4x4 attachTransform = spawnActor?.Transform ?? Matrix4x4.Identity;

            if (spawnActor == null)
                return attachTransform;

            int boneIdx = spawnActor.Skeleton.GetBoneIndex(projectileType.BoneLink.ToString(), true);

            if (boneIdx != -1)
                attachTransform = spawnActor.GetAbsoluteBoneMatrix(boneIdx);

            return attachTransform;
        }

        private static Matrix4x4 GetUserDirectionParentTransform(Actor spawnActor, Matrix4x4 attachTransform)
        {
            if (spawnActor == null)
                return attachTransform;

            Matrix4x4 parentTransform = GetRotationOnly(spawnActor.BaseTransform);
            parentTransform.Translation = attachTransform.Translation;
            return parentTransform;
        }

        private static Matrix4x4 GetRotationOnly(Matrix4x4 transform)
        {
            if (Matrix4x4.Decompose(transform, out _, out System.Numerics.Quaternion rotation, out _))
                return Matrix4x4.CreateFromQuaternion(rotation);

            transform.Translation = SimdVector3.Zero;
            return transform;
        }

        private Matrix4x4 GetCurrentProjectileParentTransform()
        {
            if (projectileType == null)
                return Matrix4x4.Identity;

            Matrix4x4 attachTransform = GetProjectileAttachTransform(attachActor, projectileType);

            if (IsUserDirectionSpawnOrientation(projectileType))
                return GetCurrentUserDirectionParentTransform(attachTransform);

            return CreateProjectileParentTransform(attachActor, projectileType, attachTransform);
        }

        private Matrix4x4 GetCurrentUserDirectionParentTransform(Matrix4x4 attachTransform)
        {
            if (attachActor == null)
                return attachTransform;

            Matrix4x4 currentAttachRotation = GetUserDirectionAttachRotation(attachActor, attachTransform);
            Matrix4x4 attachRotationDelta = GetRotationDelta(initialUserDirectionAttachRotation, currentAttachRotation);
            Matrix4x4 actorTransform = IsUserDirection3SpawnOrientation(projectileType)
                ? attachActor.Transform : attachActor.BaseTransform;
            Matrix4x4 parentTransform = attachRotationDelta * GetRotationOnly(actorTransform);
            parentTransform.Translation = attachTransform.Translation;
            return parentTransform;
        }

        private static Matrix4x4 GetUserDirectionAttachRotation(Actor actor, BAC_Type9 projectileType)
        {
            if (actor == null || projectileType == null)
                return Matrix4x4.Identity;

            Matrix4x4 attachTransform = GetProjectileAttachTransform(actor, projectileType);
            return GetUserDirectionAttachRotation(actor, attachTransform);
        }

        private static Matrix4x4 GetUserDirectionAttachRotation(Actor actor, Matrix4x4 attachTransform)
        {
            if (actor == null)
                return Matrix4x4.Identity;

            Matrix4x4 attachRotation = GetRotationOnly(attachTransform);
            Matrix4x4 actorRotation = GetRotationOnly(actor.Transform);

            if (Matrix4x4.Invert(actorRotation, out Matrix4x4 inverseActorRotation))
                return attachRotation * inverseActorRotation;

            return attachRotation;
        }

        private static Matrix4x4 GetRotationDelta(Matrix4x4 startRotation, Matrix4x4 currentRotation)
        {
            if (Matrix4x4.Invert(startRotation, out Matrix4x4 inverseStartRotation))
                return inverseStartRotation * currentRotation;

            return Matrix4x4.Identity;
        }

        private Matrix4x4 CreateWorldTransformFromMotion(Matrix4x4 localMotionTransform)
        {
            Matrix4x4 worldTransform = localMotionTransform * GetCurrentProjectileParentTransform();
            return ApplyUserDirection1(worldTransform, projectileType);
        }

        private static Matrix4x4 ApplyUserDirection1(Matrix4x4 worldTransform, BAC_Type9 projectileType)
        {
            if (!IsUserDirection1SpawnOrientation(projectileType))
                return worldTransform;

            Actor linkedActor = SceneManager.Actors[1];
            if (linkedActor == null)
                return worldTransform;

            return PointAtTarget(worldTransform, linkedActor.Transform.Translation);
        }

        private static Matrix4x4 PointAtTarget(Matrix4x4 worldTransform, SimdVector3 targetPosition)
        {
            SimdVector3 direction = targetPosition - worldTransform.Translation;
            if (direction.LengthSquared() < 0.000001f)
                return worldTransform;

            SimdVector3 forward = SimdVector3.Normalize(direction);
            SimdVector3 right = SimdVector3.Cross(forward, SimdVector3.UnitY);
            if (right.LengthSquared() < 0.000001f)
                right = SimdVector3.Cross(forward, SimdVector3.UnitZ);

            right = SimdVector3.Normalize(right);
            SimdVector3 up = SimdVector3.Cross(right, forward);
            worldTransform.M11 = right.X;
            worldTransform.M12 = right.Y;
            worldTransform.M13 = right.Z;
            worldTransform.M14 = 0f;
            worldTransform.M21 = up.X;
            worldTransform.M22 = up.Y;
            worldTransform.M23 = up.Z;
            worldTransform.M24 = 0f;
            worldTransform.M31 = -forward.X;
            worldTransform.M32 = -forward.Y;
            worldTransform.M33 = -forward.Z;
            worldTransform.M34 = 0f;
            return worldTransform;
        }

    }
}
