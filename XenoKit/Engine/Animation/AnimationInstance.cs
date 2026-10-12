using Microsoft.Xna.Framework;
using System;
using System.Linq;
using XenoKit.Editor;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ESK;
using static Xv2CoreLib.BAC.BAC_Type0;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Animation
{
    public class AnimationInstance
    {
        public EAN_File EanFile;
        public int EanIndex;
        public EAN_Animation Animation;
        /// <summary>
        /// The current frame of the animation. Being in float format allows for time scaled animations.
        /// </summary>
        public float CurrentFrame;
        /// <summary>
        /// The absolute current frame of the animation, including any "frozen" frames (ex: happens when StartFrame + EndFrame are the same, or an animation with an EndFrame well beyond what it actually has keyframes for). This is used for calculating the proper blending amount.
        /// </summary>
        private float CurrentFrameAdjusted;
        /// <summary>
        /// Why does this exist? I cant remember...
        /// </summary>
        public float PreviousFrame;
        /// <summary>
        /// The animation will start from this frame, and if looping will revert to this frame at the end of the loop.
        /// </summary>
        public int StartFrame;
        /// <summary>
        /// The end frame of the animation. If this extends beyond what the animation has keyframes for, then the final keyframed pose will be held.
        /// </summary>
        public int EndFrame;
        /// <summary>
        /// [BAC] The TimeScale value from the BAC entry that played this animation. 
        /// </summary>
        public float AnimationTimeScale;
        /// <summary>
        /// Used by Secondary Animations to automatically terminate once EndFrame is reached. Unused by the PrimaryAnimation.
        /// </summary>
        public bool AutoTerminate;
        /// <summary>
        /// [BAC] Enables the face bones of an animation.
        /// </summary>
        public bool EnableFaceBones = true;

        //These properties are for Animation Blending (previous animation frame + current animation). This is done when starting a new animation to create a more smoother transition.
        /// <summary>
        /// This is where the previous animations final pose is stored (if null, no blending will be done).
        /// </summary>
        public Matrix4x4[] PreviousAnimRelativeMatrices = null;
        /// <summary>
        /// The initial Blend Weight for this animation.
        /// </summary>
        public float BlendWeight = 1f;
        /// <summary>
        /// The ammount to increase the Blend Weight by each frame.
        /// </summary>
        public float BlendWeightIncreasePerFrame = 0f;
        public float CurrentBlendWeight => MathHelper.Clamp(BlendWeight + (BlendWeightIncreasePerFrame * (CurrentFrameAdjusted - StartFrame)), 0f, 1f);

        //World Movement
        public AnimationFlags AnimFlags;
        public bool hasMoved = false;
        public bool useTransform;

        //Properties
        public int CurrentFrame_Int
        {
            get
            {
                return (int)Math.Floor(CurrentFrame);
            }
            set
            {
                if (CurrentFrame != value)
                {
                    CurrentFrame = value;
                    CurrentFrameAdjusted = value;
                }
            }
        }
        public int CurrentAnimDuration => EndFrame - StartFrame;

        //Optimizations:
        //Here some animation-state values are saved for use in a later frame, instead of needlessly (and expensively) computing them each frame.
        /// <summary>
        /// Per animation node. This is the Keyframe index in the keyframe list (not actual frame)
        /// </summary>
        public int[] CurrentNodeFrameIndex_Pos;
        /// <summary>
        /// Per animation node. This is the Keyframe index in the keyframe list (not actual frame)
        /// </summary>
        public int[] CurrentNodeFrameIndex_Rot;
        /// <summary>
        /// Per animation node. This is the Keyframe index in the keyframe list (not actual frame)
        /// </summary>
        public int[] CurrentNodeFrameIndex_Scale;

        /// <summary>
        /// Index of b_C_Base node.
        /// </summary>
        public int b_C_Base_Index;
        /// <summary>
        /// Index of b_C_Pelvis node.
        /// </summary>
        public int b_C_Pelvis_Index;
        /// <summary>
        /// Index of f_L_EyeIris
        /// </summary>
        public int LeftEye_Index;
        /// <summary>
        /// Index of f_R_EyeIris
        /// </summary>
        public int RightEye_Index;

        public AnimationInstance(EAN_File _eanFile, int index, int startFrame = 0, int endFrame = -1, float blendWeight = 1f, float blendWeightIncrease = 0f, Matrix4x4[] previousMatrices = null, AnimationFlags _animFlags = 0, bool _useTransform = true, float timeScale = 1f, bool autoTerminate = false)
        {
            EanFile = _eanFile;
            EanIndex = index;
            StartFrame = startFrame;
            EndFrame = endFrame;
            CurrentFrame = startFrame;
            CurrentFrameAdjusted = startFrame;
            PreviousFrame = startFrame;
            Animation = EanFile.GetAnimation(index, true);
            BlendWeight = MathHelper.Clamp(blendWeight, 0f, 1f);
            BlendWeightIncreasePerFrame = MathHelper.Clamp(blendWeightIncrease, 0f, 1f);
            PreviousAnimRelativeMatrices = previousMatrices;
            AnimFlags = _animFlags;
            useTransform = _useTransform;
            AnimationTimeScale = timeScale;
            AutoTerminate = autoTerminate;

            if (Animation == null)
                Log.Add($"An animation could not be found at index: {index}", LogType.Error);

            if ((EndFrame == -1 || EndFrame == ushort.MaxValue) && Animation != null)
                EndFrame = Animation.FrameCount - 1;

            AnimationDataChanged();
        }

        public void AnimationDataChanged()
        {
            if (Animation == null) return;
            //Set indexes
            b_C_Base_Index = Animation.Nodes.IndexOf(Animation.Nodes.FirstOrDefault(x => x.BoneName == ESK_File.BaseBone));
            b_C_Pelvis_Index = Animation.Nodes.IndexOf(Animation.Nodes.FirstOrDefault(x => x.BoneName == ESK_File.PelvisBone));
            LeftEye_Index = Animation.Nodes.IndexOf(Animation.Nodes.FirstOrDefault(x => x.BoneName == ESK_File.LeftEyeIrisBone));
            RightEye_Index = Animation.Nodes.IndexOf(Animation.Nodes.FirstOrDefault(x => x.BoneName == ESK_File.RightEyeIrisBone));

            //Nodes:
            //Always recreate the arrays here as the node count may change
            CurrentNodeFrameIndex_Pos = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Rot = new int[Animation.Nodes.Count];
            CurrentNodeFrameIndex_Scale = new int[Animation.Nodes.Count];

        }

        public void ResetFrameIndex()
        {
            //Use loops instead of recreating the arrays

            for (int i = 0; i < CurrentNodeFrameIndex_Pos.Length; i++)
                CurrentNodeFrameIndex_Pos[i] = 0;

            for (int i = 0; i < CurrentNodeFrameIndex_Rot.Length; i++)
                CurrentNodeFrameIndex_Rot[i] = 0;

            for (int i = 0; i < CurrentNodeFrameIndex_Scale.Length; i++)
                CurrentNodeFrameIndex_Scale[i] = 0;
        }

        public void SkipToFrame(float frame)
        {
            PreviousFrame = frame;
            CurrentFrame = frame;
            CurrentFrameAdjusted = frame;
        }
    
        public void AdvanceFrame(bool useTimeScale, float timeScale)
        {
            if (CurrentFrame < EndFrame)
            {
                PreviousFrame = CurrentFrame;

                //We dont use TimeScale when simulating frames, so this is required
                float actualTimeScale = (useTimeScale) ? timeScale : 1f;
                CurrentFrame += 1f * actualTimeScale;
                CurrentFrameAdjusted += 1f * actualTimeScale;
            }
            else
            {
                CurrentFrameAdjusted++;
            }
        }
    }
}
