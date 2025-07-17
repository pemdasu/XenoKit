using Microsoft.Xna.Framework;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Animation
{
    public abstract class AnimationPlayerBase : Entity
    {
        protected Xv2Skeleton Skeleton;
        protected virtual bool IsUsingAnimation => false;

        public AnimationPlayerBase(GameBase game) : base(game)
        {

        }

        protected void ClearPreviousFrame()
        {
            for (int i = 0; i < Skeleton.Bones.Length; i++)
            {
                Skeleton.Bones[i].AnimationMatrix = Matrix4x4.Identity;
            }
        }

        protected void UpdateAbsoluteMatrix(Matrix4x4 rootTransform)
        {
            for (int i = 0; i < Skeleton.Bones.Length; i++)
            {
                int parentBone = i;
                Skeleton.Bones[i].AbsoluteAnimationMatrix = Matrix4x4.Identity;

                while (parentBone != -1)
                {
                    Skeleton.Bones[i].AbsoluteAnimationMatrix *= Skeleton.Bones[parentBone].AnimationMatrix * Skeleton.Bones[parentBone].RelativeMatrix * Skeleton.Bones[parentBone].BoneScaleMatrix;
                    parentBone = Skeleton.Bones[parentBone].ParentIndex;
                }
            }
        }

        protected void UpdateSkinningMatrices()
        {
            if (!IsUsingAnimation)
            {
                for (int i = 0; i < Skeleton.Bones.Length; i++)
                {
                    Skeleton.Bones[i].SkinningMatrix = Matrix4x4.Identity;
                }
            }
            else
            {
                for (int i = 0; i < Skeleton.Bones.Length; i++)
                {
                    Skeleton.Bones[i].SkinningMatrix = Skeleton.Bones[i].InverseBindPoseMatrix * Skeleton.Bones[i].AbsoluteAnimationMatrix;
                }
            }
        }

        #region Helpers
        public Matrix4x4 GetCurrentAbsoluteMatrix(string boneName)
        {
            int idx = Skeleton.GetBoneIndex(boneName);
            return Skeleton.Bones[idx > -1 ? idx : 0].AbsoluteAnimationMatrix;
        }

        /// <summary>
        /// Returns the parent absolute matrix for the specified bone, or in the case of the root bone that has no parent, <see cref="Matrix.Identity"/>.
        /// </summary>
        /// <param name="boneName"></param>
        /// <returns></returns>
        public Matrix4x4 GetCurrentParentAbsoluteMatrix(string boneName)
        {
            string parentBone = Skeleton.GetParentBone(boneName);
            return parentBone != null ? GetCurrentAbsoluteMatrix(parentBone) : Matrix4x4.Identity;
        }
        #endregion
    }
}
