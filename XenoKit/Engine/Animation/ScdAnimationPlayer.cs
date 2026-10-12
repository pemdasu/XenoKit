using System;
using Xv2CoreLib.EAN;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Animation
{
    public sealed class ScdAnimationPlayer : IDisposable
    {
        private readonly AnimationPlayer player;
        private readonly Xv2Skeleton skeleton;
        private readonly int[] charaBoneIndices;
        private bool loop;

        public bool IsActive => player.PrimaryAnimation != null;
        public float CurrentFrame => player.PrimaryAnimation?.CurrentFrame ?? 0f;

        public ScdAnimationPlayer(Xv2Skeleton skeleton, int[] charaBoneIndices)
        {
            this.skeleton = skeleton;
            this.charaBoneIndices = charaBoneIndices;
            player = new AnimationPlayer(skeleton, (Actor)null);
        }

        public bool Play(EAN_File file, int animationId, int startFrame, int endFrame, bool loop)
        {
            EAN_Animation animation = file?.GetAnimation(animationId, true);
            Stop();
            if (animation == null)
                return false;

            int lastFrame = animation.FrameCount > 0 ? animation.FrameCount - 1 : 0;
            endFrame = endFrame <= 0 ? lastFrame : Math.Min(endFrame, lastFrame);
            startFrame = Math.Max(0, Math.Min(startFrame, endFrame));
            player.PrimaryAnimation = new AnimationInstance(file, animationId, startFrame, endFrame, _useTransform: false);
            this.loop = loop;
            return true;
        }

        public void Advance(float frameStep)
        {
            AnimationInstance animation = player.PrimaryAnimation;
            if (animation == null || frameStep <= 0f)
                return;

            float frame = animation.CurrentFrame + frameStep;
            if (loop && frame > animation.EndFrame)
            {
                frame = animation.StartFrame + (frame - animation.StartFrame) % (animation.EndFrame - animation.StartFrame + 1);
                animation.ResetFrameIndex();
            }
            animation.CurrentFrame = Math.Min(frame, animation.EndFrame);
        }

        public void Apply(Xv2Skeleton charaSkeleton)
        {
            if (!IsActive)
                return;

            player.SamplePose();
            int rootIndex = skeleton.BaseBoneIndex >= 0 ? charaBoneIndices[skeleton.BaseBoneIndex] : -1;
            Matrix4x4 root = rootIndex >= 0 ? charaSkeleton.Bones[rootIndex].AbsoluteAnimationMatrix : Matrix4x4.Identity;

            for (int boneIndex = 0; boneIndex < skeleton.Bones.Length; boneIndex++)
            {
                Xv2Bone bone = skeleton.Bones[boneIndex];
                int charaBoneIndex = charaBoneIndices[boneIndex];

                //Mapped EAN nodes anchor the SCD animation to the current character pose.
                if (charaBoneIndex >= 0)
                    bone.AbsoluteAnimationMatrix = charaSkeleton.Bones[charaBoneIndex].AbsoluteAnimationMatrix;
                else
                    bone.AbsoluteAnimationMatrix = bone.AnimationMatrix * bone.RelativeMatrix *
                        (bone.ParentIndex >= 0 ? skeleton.Bones[bone.ParentIndex].AbsoluteAnimationMatrix : root);

                bone.SkinningMatrix = bone.InverseBindPoseMatrix * bone.AbsoluteAnimationMatrix;
            }
        }

        public void Stop()
        {
            player.PrimaryAnimation = null;
        }

        public void Dispose()
        {
            player.Dispose();
        }
    }
}
