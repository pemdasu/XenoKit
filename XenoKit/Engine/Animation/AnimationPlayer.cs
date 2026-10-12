using System;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Inspector.InspectorEntities;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type0;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Animation
{
    public partial class AnimationPlayer : AnimationPlayerBase, IDisposable
    {
        private readonly Actor Character;
        private readonly SkinnedInspectorEntity Inspector;

        public AnimationInstance PrimaryAnimation = null;
        public float PoseFrame { get; private set; }
        public List<AnimationInstance> SecondaryAnimations = new List<AnimationInstance>();

        protected override bool IsUsingAnimation => PrimaryAnimation != null || SecondaryAnimations.Count > 0;

        public float PrimaryCurrentFrame
        {
            get
            {
                if (PrimaryAnimation != null)
                    return PrimaryAnimation.CurrentFrame_Int;
                return 0;
            }
        }
        public float PrimaryPrevFrame
        {
            get
            {
                if (PrimaryAnimation != null)
                    return PrimaryAnimation.PreviousFrame;
                return 0;
            }
        }
        public float PrimaryDuration
        {
            get
            {
                if (PrimaryAnimation != null)
                    return PrimaryAnimation.CurrentAnimDuration;
                return 0;
            }
        }

        public AnimationPlayer(Xv2Skeleton skeleton, Actor chara)
        {
            Skeleton = skeleton ?? throw new ArgumentNullException("skeleton", "AnimationPlayer: skeleton was null. Cannot construct an AnimationPlayer.");
            Character = chara;
            Inspector = null;

            SceneManager.AnimationDataChanged += SceneManager_AnimationDataChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            SceneManager.SeekOccurred += SceneManager_SeekOccurred;
        }

        public AnimationPlayer(Xv2Skeleton skeleton, SkinnedInspectorEntity skinnedEntity)
        {
            Skeleton = skeleton ?? throw new ArgumentNullException("skeleton", "AnimationPlayer: skeleton was null. Cannot construct an AnimationPlayer.");
            Inspector = skinnedEntity;
            Character = null;

            SceneManager.AnimationDataChanged += SceneManager_AnimationDataChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            SceneManager.SeekOccurred += SceneManager_SeekOccurred;
        }

        public void Dispose()
        {
            SceneManager.AnimationDataChanged -= SceneManager_AnimationDataChanged;
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            SceneManager.SeekOccurred -= SceneManager_SeekOccurred;
        }


        private void SceneManager_SeekOccurred(object sender, EventArgs e)
        {
            //When Seek happens the FrameIndex caches are no longer valid and need to be reset.

            if (PrimaryAnimation != null)
                PrimaryAnimation.ResetFrameIndex();

            foreach (var anim in SecondaryAnimations)
                anim.ResetFrameIndex();
        }

        private void Instance_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            if (e.UndoGroup == UndoGroup.Animation)
                UpdateAnimationData();
        }

        private void SceneManager_AnimationDataChanged(object sender, EventArgs e)
        {
            UpdateAnimationData();
        }

        private void UpdateAnimationData()
        {
            //Update EndFrame to match Animation.FrameCount
            if (PrimaryAnimation != null)
            {
                PrimaryAnimation.EndFrame = (PrimaryAnimation.Animation.FrameCount > 0) ? PrimaryAnimation.Animation.FrameCount - 1 : 0;
                PrimaryAnimation.AnimationDataChanged();
            }
        }

        #region FrameUpdating
        public void Update(Matrix4x4 rootTransform)
        {
            UpdatePose(rootTransform);

            //Remove finished anims
            HandleFinishedAnimations();

            //Advance frame
            if (ViewportInstance.IsPlaying && IsUsingAnimation && PrimaryAnimation?.CurrentFrame < PrimaryAnimation?.EndFrame &&
                SceneManager.IsOnTab(EditorTabs.Animation, EditorTabs.Action, EditorTabs.Projectile, EditorTabs.Inspector, EditorTabs.InspectorAnimation))
            {
                AdvanceFrame();
            }
        }

        public void UpdatePose(Matrix4x4 rootTransform)
        {
            SamplePose();
            UpdateAbsoluteMatrix(rootTransform);
            UpdateSkinningMatrices();
        }

        public void SamplePose()
        {
            ClearPreviousFrame();
            PoseFrame = PrimaryAnimation?.CurrentFrame ?? 0f;

            if (PrimaryAnimation != null)
                UpdateAnimation(PrimaryAnimation);

            for (int animationIndex = 0; animationIndex < SecondaryAnimations.Count; animationIndex++)
                UpdateAnimation(SecondaryAnimations[animationIndex]);
        }

        /// <param name="useTimeScale">Advance like live playback: scaled by the actor's time scale, and not once the primary animation has ended.</param>
        public void Simulate(bool fullUpdate, bool advance, bool useTimeScale = false)
        {
            if (fullUpdate)
            {
                Update(Matrix4x4.Identity);
            }
            else
            {
                //Update the base node only, skiping all other bones (not needed).
                if(PrimaryAnimation != null)
                {
                    if (PrimaryAnimation.b_C_Base_Index != -1)
                        UpdateNode(PrimaryAnimation, PrimaryAnimation.b_C_Base_Index);
                }

                HandleFinishedAnimations();
            }

            //Advance frame
            if (advance && IsUsingAnimation && (!useTimeScale || PrimaryAnimation?.CurrentFrame < PrimaryAnimation?.EndFrame))
            {
                AdvanceFrame(useTimeScale);
            }
        }

        private void HandleFinishedAnimations()
        {
            if (PrimaryAnimation != null)
            {
                if (PrimaryAnimation.CurrentFrame >= PrimaryAnimation.EndFrame)
                {
                    if (SceneManager.Loop && PrimaryAnimation.AutoTerminate && (ViewportInstance.IsPlaying && SceneManager.IsOnTab(EditorTabs.Animation, EditorTabs.Inspector, EditorTabs.InspectorAnimation, EditorTabs.CAC) || !SceneManager.IsOnTab(EditorTabs.Animation)))
                    {
                        PrimaryAnimation.CurrentFrame_Int = PrimaryAnimation.StartFrame;
                    }
                    else if (SceneManager.IsOnTab(EditorTabs.Animation, EditorTabs.Inspector, EditorTabs.InspectorAnimation))
                    {
                        SceneManager.Pause();
                    }
                }

            }

            for (int i = SecondaryAnimations.Count - 1; i >= 0; i--)
            {
                if (SecondaryAnimations[i].CurrentFrame >= SecondaryAnimations[i].EndFrame)
                {
                    if (SecondaryAnimations[i].AutoTerminate)
                    {
                        SecondaryAnimations.RemoveAt(i);
                        continue;
                    }
                }
            }

        }

        private void AdvanceFrame(bool useTimeScale = true)
        {
            if (Character?.Controller?.FreezeActionFrames > 0) return;

            //Viewer Mode doesn't have a Character reference
            float timeScale = Character != null ? Character.ActiveTimeScale : 1f;

            if (PrimaryAnimation != null)
            {
                PrimaryAnimation.AdvanceFrame(useTimeScale, timeScale);
            }

            for (int i = 0; i < SecondaryAnimations.Count; i++)
            {
                SecondaryAnimations[i].AdvanceFrame(useTimeScale, timeScale);
            }
        }

        /// <summary>
        /// Undo current animation b_C_Base transformation changes.
        /// </summary>
        /// <param name="frame">The frame from which to undo from.</param>
        /// <param name="undoAll">Undo everything, including ignoring bac flags.</param>
        private void UndoBasePosition(float frame, bool undoAll = false)
        {
            //Undo animation movement, if needed.
            if (PrimaryAnimation == null || Character == null) return;
            if (!PrimaryAnimation.hasMoved) return; //Safeguard

            if (undoAll)
            {
                Character.ActionMovementTransform = Matrix4x4.Identity;
                Character.RootMotionTransform = Matrix4x4.Identity;
            }
            else
            {
                Character.MergeTransforms();
            }
            /*
            //Undo all rotation
            //Undo the positions (as required by flags)
            //Redo the rotation

            var node = PrimaryAnimation.Animation.GetNode(ESK_File.BaseBone);
            if (node == null) return;

            var pos = node.GetComponent(EAN_AnimationComponent.ComponentType.Position);

            float firstPosX = 0;
            float firstPosY = 0;
            float firstPosZ = 0;
            float firstPosW = 0;
            float PosX = 0;
            float PosY = 0;
            float PosZ = 0;
            float PosW = 0;

            if (pos != null)
            {
                firstPosX = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.X);
                firstPosY = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Y);
                firstPosZ = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.Z);
                firstPosW = pos.GetKeyframeValue(PrimaryAnimation.StartFrame, Axis.W);
                PosX = pos.GetKeyframeValue(frame, Axis.X);
                PosY = pos.GetKeyframeValue(frame, Axis.Y);
                PosZ = pos.GetKeyframeValue(frame, Axis.Z);
                PosW = pos.GetKeyframeValue(frame, Axis.W);
            }

            Matrix transformSum = Matrix.Identity;

            //Position
            if (!PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_X))
            {
                transformSum *= PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion) ? Matrix.CreateTranslation(-PosX, 0, 0) : Matrix.CreateTranslation(-(PosX - firstPosX), 0, 0);
            }

            if (!PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_Y))
            {
                transformSum *= PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion) ? Matrix.CreateTranslation(0, -PosY, 0) : Matrix.CreateTranslation(0, -(PosY - firstPosY), 0);
            }

            if (!PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.MoveWithAxis_Z))
            {
                transformSum *= PrimaryAnimation.AnimFlags.HasFlag(AnimationFlags.UseRootMotion) ? Matrix.CreateTranslation(0, 0, -PosZ) : Matrix.CreateTranslation(0, 0, -(PosZ - firstPosZ));
            }

            Character.ActionMovementTransform *= transformSum;
            Character.MergeTransforms();
            */
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Clear the current animation and its state.
        /// </summary>
        public void ClearCurrentAnimation(bool removeSecondary = false, bool undoPosition = false)
        {
            if (PrimaryAnimation != null && undoPosition && Character != null)
                UndoBasePosition(PrimaryAnimation.CurrentFrame, true);

            PrimaryAnimation = null;

            if (removeSecondary)
                SecondaryAnimations.Clear();
        }

        public void ClearSecondaryAnimations()
        {
            SecondaryAnimations.Clear();
        }

        /// <summary>
        /// Load and play an animation.
        /// </summary>
        /// <param name="_eanFile">ean file to use animation from.</param>
        /// <param name="eanIndex">Index of animation in eanFile.</param>
        /// <param name="startFrame">Frame to start playback from.</param>
        /// <param name="endFrame">Frame to end the animation at. Note: -1 = play until animation ends.</param>
        /// <param name="timeScale">Playback speed of the animation. A value of 1 means normal playback.</param>
        public void PlayPrimaryAnimation(EAN_File _eanFile, int eanIndex, ushort startFrame = 0, ushort endFrame = ushort.MaxValue, float blendWeight = 1f, float blendWeightIncrease = 0f, AnimationFlags _animFlags = 0, bool useTransform = true, float timeScale = 1f, bool autoTerminate = false, bool primaryBacAnimation = false)
        {
            if (_eanFile == null) throw new ArgumentNullException("eanFile");

            if (_eanFile.AnimationExists(eanIndex))
            {
                if (PrimaryAnimation != null && Character != null)
                    UndoBasePosition(PrimaryAnimation.CurrentFrame, false);

                Matrix4x4[] prevMatrices = (PrimaryAnimation != null) ? Skeleton.GetAnimationMatrices() : null;

                PrimaryAnimation = new AnimationInstance(_eanFile, eanIndex, startFrame, endFrame, blendWeight, blendWeightIncrease, prevMatrices, _animFlags, useTransform, timeScale, autoTerminate);

                //BAC body animations do not drive face bones unless a face row asks for it
                PrimaryAnimation.EnableFaceBones = !primaryBacAnimation;

                //Render first frame if not auto playing
                if (!ViewportInstance.IsPlaying)
                    UpdateAnimation(PrimaryAnimation);
            }
            else
            {
                Log.Add($"An animation could not be found at index: {eanIndex}", LogType.Error);
            }
        }

        /// <summary>
        /// Load and play a secondary animation.
        /// </summary>
        /// <param name="_eanFile">ean file to use animation from.</param>
        /// <param name="eanIndex">Index of animation in eanFile.</param>
        /// <param name="startFrame">Frame to start playback from.</param>
        /// <param name="endFrame">Frame to end the animation at. Note: -1 = play until animation ends.</param>
        /// <param name="timeScale">Playback speed of the animation. A value of 1 means normal playback.</param>
        public void PlaySecondaryAnimation(EAN_File _eanFile, int eanIndex, ushort startFrame = 0, ushort endFrame = ushort.MaxValue, float blendWeight = 1f, float blendWeightIncrease = 0f, float timeScale = 1f, bool autoTerminate = false)
        {
            if (_eanFile == null) throw new ArgumentNullException("eanFile");

            Matrix4x4[] prevMatrices = (PrimaryAnimation != null) ? Skeleton.GetAnimationMatrices() : null;

            SecondaryAnimations.Add(new AnimationInstance(_eanFile, eanIndex, startFrame, endFrame, blendWeight, blendWeightIncrease, prevMatrices, 0, true, timeScale, autoTerminate));
        }

        public void GoToFrame(int frame, bool mergeTransforms)
        {
            if (PrimaryAnimation == null) return;

            if (mergeTransforms)
            {
                Character.MergeTransforms();
            }

            PrimaryAnimation.CurrentFrame = frame;
        }
        #endregion

        #region Helpers

        public void Resume()
        {
            if (PrimaryCurrentFrame >= PrimaryDuration && PrimaryAnimation != null && SceneManager.IsOnTab(EditorTabs.Animation))
            {
                PrimaryAnimation.CurrentFrame_Int = 0;
            }
        }

        public void NextFrame()
        {
            if (PrimaryAnimation == null) return;
            if (PrimaryAnimation.CurrentFrame_Int < PrimaryAnimation.EndFrame)
            {
                PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;
                PrimaryAnimation.CurrentFrame_Int++;
            }
            else if (PrimaryAnimation.CurrentFrame_Int >= PrimaryAnimation.EndFrame)
            {
                PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;
                PrimaryAnimation.CurrentFrame_Int = PrimaryAnimation.StartFrame;
            }

            Update(Matrix4x4.Identity);
        }

        public void PrevFrame()
        {
            if (PrimaryAnimation == null) return;
            if (PrimaryAnimation.CurrentFrame_Int > PrimaryAnimation.StartFrame)
            {
                PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;
                PrimaryAnimation.CurrentFrame_Int--;
            }
            else if (PrimaryAnimation.CurrentFrame_Int <= PrimaryAnimation.StartFrame)
            {
                PrimaryAnimation.PreviousFrame = PrimaryAnimation.CurrentFrame;
                PrimaryAnimation.CurrentFrame_Int = PrimaryAnimation.EndFrame;
            }

            Update(Matrix4x4.Identity);
        }

        public void FirstFrame()
        {
            if (PrimaryAnimation?.CurrentFrame > PrimaryAnimation?.StartFrame)
            {
                PrimaryAnimation.CurrentFrame_Int = PrimaryAnimation.StartFrame;
            }
        }

        #endregion
    }

}
