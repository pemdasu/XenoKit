using Microsoft.Xna.Framework;
using XenoKit.Helper;
using Xv2CoreLib;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ESK;
using static Xv2CoreLib.BAC.BAC_Type0;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Animation
{
    public partial class AnimationPlayer
    {
        #region BoneUpdating
        private void UpdateAnimation(AnimationInstance animation)
        {
            if (animation.Animation == null) return;

            for (int i = 0; i < animation.Animation.Nodes.Count; i++)
            {
                UpdateNode(animation, i);
            }
        }

        private void UpdateNode(AnimationInstance animation, int animNodeIndex)
        {
            var node = animation.Animation.Nodes[animNodeIndex];
            int boneIdx = Skeleton.GetBoneIndex(node.BoneName);
            if (boneIdx == -1) //Bone doesn't exist in character ESK, so skip
                return;

            //Skip face bones if face animations are not allowed on this animation. Used for BAC animations which by default dont use face bones.
            if (Skeleton.Bones[boneIdx].IsFaceBone && !animation.EnableFaceBones)
            {
                //Rest pose, so a face pose from the last animation does not stay
                Skeleton.Bones[boneIdx].AnimationMatrix = Matrix4x4.Identity;
                return;
            }

            ESK_Bone bone = animation.EanFile.Skeleton.GetBone(node.BoneName); // Bone from Ean file, we have to revert the relative Transform before we apply animation values (because animations values are for the inside ean file skeleton first)
            ESK_RelativeTransform transform = bone.RelativeTransform;

            SimdVector3 ean_initialBonePosition = new SimdVector3(transform.PositionX, transform.PositionY, transform.PositionZ) * transform.PositionW;
            SimdQuaternion ean_initialBoneOrientation = new SimdQuaternion(transform.RotationX, transform.RotationY, transform.RotationZ, transform.RotationW);
            SimdVector3 ean_initialBoneScale = new SimdVector3(transform.ScaleX, transform.ScaleY, transform.ScaleZ) * transform.ScaleW;

            //Face keyframes use the character's local pose, even when the FCE skeleton has a different rest pose.
            if (Skeleton.Bones[boneIdx].IsFaceBone)
                Matrix4x4.Decompose(Skeleton.Bones[boneIdx].RelativeMatrix, out ean_initialBoneScale, out ean_initialBoneOrientation, out ean_initialBonePosition);

            //Scale animations to fit current actor size
            if (!animation.EanFile.IsCharaUnique && animNodeIndex == animation.b_C_Pelvis_Index && Character != null)
            {
                ean_initialBonePosition.Y -= (Character.CharacterData.BcsFile.File.F_48[0] - 1f) / 2f;
            }

            Matrix4x4 relativeMatrix_EanBone_inv = Matrix4x4.Identity;
            relativeMatrix_EanBone_inv *= Matrix4x4.CreateScale(ean_initialBoneScale);
            relativeMatrix_EanBone_inv *= Matrix4x4.CreateFromQuaternion(ean_initialBoneOrientation);
            relativeMatrix_EanBone_inv *= Matrix4x4.CreateTranslation(ean_initialBonePosition);
            Matrix4x4.Invert(relativeMatrix_EanBone_inv, out relativeMatrix_EanBone_inv);

            if (animNodeIndex == animation.b_C_Base_Index && animation.useTransform)
            {
                UpdateBasePosition(node, ean_initialBoneScale);
                Skeleton.Bones[boneIdx].AnimationMatrix = Matrix4x4.Identity;
            }
            else
            {
                //Return values for current frame index
                int frameIndex_Pos = 0;
                int frameIndex_Rot = 0;
                int frameIndex_Scale = 0;

                //Read components

                var pos = node.GetComponent(EAN_AnimationComponent.ComponentType.Position);
                var rot = node.GetComponent(EAN_AnimationComponent.ComponentType.Rotation);
                var scale = node.GetComponent(EAN_AnimationComponent.ComponentType.Scale);

                Matrix4x4 transformAnimation = Matrix4x4.Identity;

                //Scale:
                SimdVector3 scale_tmp = ean_initialBoneScale;

                if (scale?.Keyframes.Count > 0)
                {
                    float x = scale.GetKeyframeValue(animation.CurrentFrame, Axis.X, ref frameIndex_Scale, animation.CurrentNodeFrameIndex_Scale[animNodeIndex]);
                    float y = scale.GetKeyframeValue(animation.CurrentFrame, Axis.Y, ref frameIndex_Scale, animation.CurrentNodeFrameIndex_Scale[animNodeIndex]);
                    float z = scale.GetKeyframeValue(animation.CurrentFrame, Axis.Z, ref frameIndex_Scale, animation.CurrentNodeFrameIndex_Scale[animNodeIndex]);
                    float w = scale.GetKeyframeValue(animation.CurrentFrame, Axis.W, ref frameIndex_Scale, animation.CurrentNodeFrameIndex_Scale[animNodeIndex]);

                    scale_tmp = new SimdVector3(x, y, z) * w;
                }

                transformAnimation *= Matrix4x4.CreateScale(scale_tmp);

                //Rotation:
                SimdQuaternion quat_tmp = ean_initialBoneOrientation;
                if (rot?.Keyframes.Count > 0)
                {
                    float x = rot.GetKeyframeValue(animation.CurrentFrame, Axis.X, ref frameIndex_Rot, animation.CurrentNodeFrameIndex_Rot[animNodeIndex]);
                    float y = rot.GetKeyframeValue(animation.CurrentFrame, Axis.Y, ref frameIndex_Rot, animation.CurrentNodeFrameIndex_Rot[animNodeIndex]);
                    float z = rot.GetKeyframeValue(animation.CurrentFrame, Axis.Z, ref frameIndex_Rot, animation.CurrentNodeFrameIndex_Rot[animNodeIndex]);
                    float w = rot.GetKeyframeValue(animation.CurrentFrame, Axis.W, ref frameIndex_Rot, animation.CurrentNodeFrameIndex_Rot[animNodeIndex]);

                    quat_tmp = new SimdQuaternion(x, y, z, w);
                }

                transformAnimation *= Matrix4x4.CreateFromQuaternion(quat_tmp);

                //Position:
                SimdVector3 pos_tmp = ean_initialBonePosition;

                if (animNodeIndex != animation.b_C_Pelvis_Index || (animation.AnimFlags & AnimationFlags.UseRootMotion) != AnimationFlags.UseRootMotion)
                {
                    //b_C_Pelvis position is skipped with UseRootMotion flag

                    if (pos?.Keyframes.Count > 0)
                    {
                        float x = pos.GetKeyframeValue(animation.CurrentFrame, Axis.X, ref frameIndex_Pos, animation.CurrentNodeFrameIndex_Pos[animNodeIndex]);
                        float y = pos.GetKeyframeValue(animation.CurrentFrame, Axis.Y, ref frameIndex_Pos, animation.CurrentNodeFrameIndex_Pos[animNodeIndex]);
                        float z = pos.GetKeyframeValue(animation.CurrentFrame, Axis.Z, ref frameIndex_Pos, animation.CurrentNodeFrameIndex_Pos[animNodeIndex]);
                        float w = pos.GetKeyframeValue(animation.CurrentFrame, Axis.W, ref frameIndex_Pos, animation.CurrentNodeFrameIndex_Pos[animNodeIndex]);

                        pos_tmp = new SimdVector3(x, y, z) * w;
                    }

                    transformAnimation *= Matrix4x4.CreateTranslation(pos_tmp);
                }

                //Previous animation blending:
                if (animation.PreviousAnimRelativeMatrices == null || animation.CurrentBlendWeight >= 1f || boneIdx == Skeleton.BaseBoneIndex)
                {
                    //No blending if blendWeight is 1, is on b_C_Base or if no previous matrices data is present
                    Skeleton.Bones[boneIdx].AnimationMatrix = transformAnimation * relativeMatrix_EanBone_inv;
                }
                else
                {
                    //Enable animation blending with previous animation matrices
                    Skeleton.Bones[boneIdx].AnimationMatrix = GeneralHelpers.SlerpMatrix(animation.PreviousAnimRelativeMatrices[boneIdx], transformAnimation * relativeMatrix_EanBone_inv, animation.CurrentBlendWeight);
                }

                //Update saved frame index.
                animation.CurrentNodeFrameIndex_Pos[animNodeIndex] = frameIndex_Pos;
                animation.CurrentNodeFrameIndex_Rot[animNodeIndex] = frameIndex_Rot;
                animation.CurrentNodeFrameIndex_Scale[animNodeIndex] = frameIndex_Scale;

                //Handle EYE animations
                if(Character?.BacEyeMovementUsed == false && Character != null)
                {
                    //Apply iris animation onto the Actor reference
                    if (animNodeIndex == animation.LeftEye_Index)
                    {
                        Character.EyeIrisLeft_UV[0] = -(pos_tmp.X * 10);
                        Character.EyeIrisLeft_UV[1] = -(pos_tmp.Y * 10);
                    }

                    if (animNodeIndex == animation.RightEye_Index)
                    {
                        Character.EyeIrisRight_UV[0] = -(pos_tmp.X * 10);
                        Character.EyeIrisRight_UV[1] = -(pos_tmp.Y * 10);
                    }
                }
                else if(Inspector != null)
                {
                    //Apply iris animation onto the SkinnedInspectorEntity reference
                    if (animNodeIndex == animation.LeftEye_Index)
                    {
                        Inspector.EyeIrisLeft_UV[0] = -(pos_tmp.X * 10);
                        Inspector.EyeIrisLeft_UV[1] = -(pos_tmp.Y * 10);
                    }

                    if (animNodeIndex == animation.RightEye_Index)
                    {
                        Inspector.EyeIrisRight_UV[0] = -(pos_tmp.X * 10);
                        Inspector.EyeIrisRight_UV[1] = -(pos_tmp.Y * 10);
                    }
                }
            }
        }

        private void UpdateBasePosition(EAN_Node _base, SimdVector3 ean_initialBoneScale)
        {
            //Seperate code path for handling b_C_Base when transform is enabled
            //This will take movement flags into account and properly translate the characters world position
            EAN_AnimationComponent pos = _base.GetComponent(EAN_AnimationComponent.ComponentType.Position);
            EAN_AnimationComponent rot = _base.GetComponent(EAN_AnimationComponent.ComponentType.Rotation);

            float firstPosX = 0;
            float firstPosY = 0;
            float firstPosZ = 0;
            float firstPosW = 0;
            float firstRotX = 0;
            float firstRotY = 0;
            float firstRotZ = 0;
            float firstRotW = 0;
            float PosX = 0;
            float PosY = 0;
            float PosZ = 0;
            float PosW = 0;
            float RotX = 0;
            float RotY = 0;
            float RotZ = 0;
            float RotW = 0;

            if (pos != null)
            {
                firstPosX = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.X);
                firstPosY = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Y);
                firstPosZ = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Z);
                firstPosW = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.W);
                PosX = pos.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.X);
                PosY = pos.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.Y);
                PosZ = pos.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.Z);
                PosW = pos.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.W);
            }

            if (rot != null)
            {
                firstRotX = rot.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.X);
                firstRotY = rot.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Y);
                firstRotZ = rot.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Z);
                firstRotW = rot.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.W);
                RotX = rot.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.X);
                RotY = rot.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.Y);
                RotZ = rot.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.Z);
                RotW = rot.GetKeyframeValue(PrimaryAnimation.CurrentFrame, Axis.W);
            }

            Matrix4x4 transformSum = Matrix4x4.CreateScale(ean_initialBoneScale);
            Matrix4x4 rootMotion = Matrix4x4.CreateScale(ean_initialBoneScale);

            //Rotation is always done regardless of flags:
            transformSum *= Matrix4x4.CreateFromQuaternion(new SimdQuaternion(RotX, RotY, RotZ, RotW));
            transformSum *= Matrix4x4.CreateFromQuaternion(new SimdQuaternion(-firstRotX, -firstRotY, -firstRotZ, -firstRotW));

            //NOTE: Rotation seems a little off. Works fine when everything is movement, but breaks on root motion

            if ((PrimaryAnimation.AnimFlags & AnimationFlags.Rotate180Degrees) == AnimationFlags.Rotate180Degrees)
            {
                transformSum *= Matrix4x4.CreateRotationY(MathHelper.ToRadians(180f));
            }

            //Root motion only. Ignores other flags.
            if ((PrimaryAnimation.AnimFlags & AnimationFlags.UseRootMotion) == AnimationFlags.UseRootMotion)
            {
                //Set absolute values
                rootMotion *= Matrix4x4.CreateTranslation(new SimdVector3(PosX, PosY, PosZ) * PosW);

                //Subtract the first frame. This is because no matter what position is on the first frame, its considered the starting point and the game will essentially ignore it and treat it as if it were at 0,0,0 (except in a special case with the movement flags)
                rootMotion *= Matrix4x4.CreateTranslation(new SimdVector3(-firstPosX, -firstPosY, -firstPosZ) * firstPosW);
            }
            else
            {
                //X Axis
                if ((PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_X) == AnimationFlags.MoveWithAxis_X)
                {
                    //Movement is enabled
                    transformSum *= Matrix4x4.CreateTranslation(new SimdVector3(PosX, 0, 0) * PosW);
                    transformSum *= Matrix4x4.CreateTranslation(new SimdVector3(-firstPosX, 0, 0) * firstPosW);

                }
                else if ((PrimaryAnimation.AnimFlags & AnimationFlags.IgnoreRootMotionX) != AnimationFlags.IgnoreRootMotionX)
                {
                    //Root motion only
                    rootMotion *= Matrix4x4.CreateTranslation(new SimdVector3(PosX, 0, 0) * PosW);

                    //If the any other axis have the movement flag and this axis doesn't, then Full Root Motion is applied (don't know why)
                    if ((PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_Y) != AnimationFlags.MoveWithAxis_Y && 
                        (PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_Z) != AnimationFlags.MoveWithAxis_Z)
                    {
                        rootMotion *= Matrix4x4.CreateTranslation(new SimdVector3(-firstPosX, 0, 0) * firstPosW);
                    }
                }

                //Y Axis
                if ((PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_Y) == AnimationFlags.MoveWithAxis_Y && 
                    (PrimaryAnimation.AnimFlags & AnimationFlags.ForceYRootMotion) != AnimationFlags.ForceYRootMotion)
                {
                    //Movement is enabled
                    transformSum *= Matrix4x4.CreateTranslation(new SimdVector3(0, PosY, 0) * PosW);
                    transformSum *= Matrix4x4.CreateTranslation(new SimdVector3(0, -firstPosY, 0) * firstPosW);

                }
                else if ((PrimaryAnimation.AnimFlags & AnimationFlags.IgnoreRootMotionY) != AnimationFlags.IgnoreRootMotionY
                    || (PrimaryAnimation.AnimFlags & AnimationFlags.ForceYRootMotion) == AnimationFlags.ForceYRootMotion)
                {
                    //Root motion only
                    rootMotion *= Matrix4x4.CreateTranslation(new SimdVector3(0, PosY, 0) * PosW);

                    //If the any other axis have the movement flag and this axis doesn't, then Full Root Motion is applied (don't know why)
                    if ((PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_X) != AnimationFlags.MoveWithAxis_X && 
                        (PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_Z) != AnimationFlags.MoveWithAxis_Z)
                    {
                        rootMotion *= Matrix4x4.CreateTranslation(new SimdVector3(0, -firstPosY, 0) * firstPosW);
                    }
                }

                //Z Axis
                if ((PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_Z) == AnimationFlags.MoveWithAxis_Z)
                {
                    //Movement is enabled
                    transformSum *= Matrix4x4.CreateTranslation(new SimdVector3(0, 0, PosZ) * PosW);
                    transformSum *= Matrix4x4.CreateTranslation(new SimdVector3(0, 0, -firstPosZ) * firstPosW);

                }
                else if ((PrimaryAnimation.AnimFlags & AnimationFlags.IgnoreRootMotionZ) != AnimationFlags.IgnoreRootMotionZ)
                {
                    //Root motion only
                    rootMotion *= Matrix4x4.CreateTranslation(new SimdVector3(0, 0, PosZ) * PosW);

                    //If the any other axis have the movement flag and this axis doesn't, then Full Root Motion is applied (don't know why)
                    if ((PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_X) != AnimationFlags.MoveWithAxis_X
                        && (PrimaryAnimation.AnimFlags & AnimationFlags.MoveWithAxis_Y) != AnimationFlags.MoveWithAxis_Y)
                    {
                        rootMotion *= Matrix4x4.CreateTranslation(new SimdVector3(0, 0, -firstPosZ) * firstPosW);
                    }
                }
            }

            if(Character != null)
            {
                Character.ActionMovementTransform = transformSum;
                Character.RootMotionTransform = rootMotion;
                PrimaryAnimation.hasMoved = true;
            }
        }
        #endregion

    }
}
