using System;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC.Simulation;
using XenoKit.Engine.View;
using Xv2CoreLib.BAC;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Scripting.BAC
{
    public partial class BacPlayer : EngineObject
    {
        //Parent character that this BacPlayer is for
        private readonly Actor character;
        private readonly BacScreenEffectEvaluator screenEffectEvaluator;

        //Bac Entry:
        public BacEntryInstance BacEntryInstance { get; private set; }
        public bool HasBacEntry => BacEntryInstance != null;
        public bool IsPreview => BacEntryInstance?.IsPreview == true;
        public bool DelayedResimulate { get; set; }

        //Frame and Duration:
        public int CurrentFrame
        {
            get
            {
                if (BacEntryInstance == null) return 0;
                return (int)Math.Max(BacEntryInstance.CurrentFrame, 0f);
            }
            set
            {
                BacEntryInstance.CurrentFrame = value;
            }
        }

        public int CurrentDuration
        {
            get
            {
                if (BacEntryInstance == null) return 0;
                return BacEntryInstance.Duration;
            }
        }
        public float ScaledDuration
        {
            get => BacEntryInstance != null ? BacEntryInstance.ScaledDuration : 0f;
        }
        public bool IsScaled
        {
            get => BacEntryInstance != null ? BacEntryInstance.HasTimeScale : false;
        }

        public static bool SimulateMovement { get; set; } = true;
        public static bool SimulateTracking { get; set; } = true;

        //Stops a seek or step when the BAC frame does not move, such as with a time scale of 0.
        private const int MaxSeekTicks = 60 * 60 * 10;
        private const int MaxStepTicks = 60;

        //Threading:
        /// <summary>
        /// For thread-safety. Updating wont happen when this is set, which prevents a BAC entry that has just been removed from being accessed.
        /// </summary>
        private bool clearing = false;

        public BacPlayer(Actor chara)
        {
            character = chara;
            screenEffectEvaluator = new BacScreenEffectEvaluator(chara);
            Xv2CoreLib.Resource.UndoRedo.UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object source, Xv2CoreLib.Resource.UndoRedo.UndoEventRaisedEventArgs e)
        {
            if (BacEntryInstance != null)
            {
                BacEntryInstance.RefreshDetails();
            }
        }

        public void PlayBacEntry(BAC_File bacFile, BAC_Entry bacEntry, Actor user, Move move)
        {
            if (bacEntry == null)
            {
                Log.Add("BacPlayer.PlayBacEntry: bacEntry was null.");
                return;
            }

            ClearBacEntry();
            BacEntryInstance = new BacEntryInstance(bacFile, bacEntry, move, user, character, false);
        }

        public void PlayBacEntryPreview(BAC_File bacFile, BAC_Entry bacEntry, Actor user, Move move = null)
        {
            if (bacEntry == null)
            {
                Log.Add("BacPlayer.PlayBacEntryPreview: bacEntry was null.");
                return;
            }

            ResetBacPreviewState();
            ClearBacEntry();
            BacEntryInstance = new BacEntryInstance(bacFile, bacEntry, move, user, character, true);
        }

        public override void Update()
        {
            if (clearing || BacEntryInstance == null || character.Controller.FreezeActionFrames > 0) return;

            bool isPlaying = ViewportInstance.IsPlaying;

            //A new entry runs its first frame even while paused, so the actor shows it right away.
            if (isPlaying || BacEntryInstance.CurrentFrame < 0f)
                BacEntryInstance.AdvanceFrame();

            RunCurrentFrame(true, isPlaying ? character.ActiveTimeScale : 0f, isPlaying);
        }

        /// <param name="isTick">True when this call is one game tick. Paused redraws are not ticks.</param>
        private void RunCurrentFrame(bool allowLoop, float frameStep, bool isTick)
        {
            if (allowLoop)
                UpdateBacLoop();

            BacEntryInstance.ClearActiveBsaPassConditions();
            character.CanBeTracked = true;

            if (isTick)
                character.PartSet.AdvanceScdAnimations(frameStep);

            UpdateBac(frameStep, isTick);

            BacEntryInstance.Update(isTick ? character.BacTimeScale * character.BdmTimeScale : 0f);
        }

        public override void DelayedUpdate()
        {
            if (DelayedResimulate && IsPreview)
            {
                DelayedResimulate = false;
                ResimulateCurrentEntry();
            }
        }

        private void UpdateBacLoop()
        {
            if (BacEntryInstance.LoopIsDirty)
            {
                bool wasPlaying = ViewportInstance.IsPlaying;
                ViewportInstance.IsPlaying = false;
                BacEntryInstance.LoopIsDirty = false;
                Seek(BacEntryInstance.CurrentFrame);
                ViewportInstance.IsPlaying = wasPlaying;
            }
            else if (BacEntryInstance.LoopEnabled && BacEntryInstance.CurrentFrame >= BacEntryInstance.LoopEndFrame && (!IsPreview || SceneManager.AllowBacLoop))
            {
                BacEntryInstance.CurrentFrame = BacEntryInstance.LoopStartFrame;
                BacEntryInstance.CurrentLoop++;

                //Both the current animation the camera get restored back to the frame they were at when the loop was first started.
                //If the camera changes, then the wrong camera frame will be used. This is how it works in game too.
                //However, it is impossible for the animation to change within a loop, as each new primary animation will also start a new loop, effectively overwriting any Function Loops
                character.AnimationPlayer.GoToFrame(BacEntryInstance.LoopAnimationStartFrame, false);
                Viewport.Instance.Camera.SkipToFrame(BacEntryInstance.LoopCameraStartFrame);
            }
        }

        /// <summary>
        /// Simulate the bac entry up to the specified frame. This will also simulate any animations, cameras, hitboxes, projectiles and so on.
        /// </summary>
        public void Seek(float frame, bool clearAnimations = true)
        {
            if (clearing) return;

            //Get camera state so it can be restored after resimulating the bac state. 
            CameraState originalCameraState = Viewport.Instance.Camera.CameraState.Copy();

            //Calculate the number of "blending" frames between animations. With this we dont need to calculate every single animation frame, just what is needed.
            int numBlendingFrames = BAC_Type0.CalculateNumOfBlendingFrames(BacEntryInstance.BacEntry.IBacTypes, (int)frame);

            //Clean up
            SceneManager.Actors[1]?.ResetState(false);
            RevertCharacterPosition(true);

            if (clearAnimations)
                character.AnimationPlayer.ClearCurrentAnimation(true);

            Xv2CoreLib.Random.ResetWithCurrentSeed();
            VfxManager.StopEffects();
            VfxManager.ForceEffectUpdate = false;
            Viewport.Instance.Camera.ClearCameraAnimation();
            Viewport.Instance.Camera.BuiltInCameras.Clear();
            character.AnimationTimeScale = 1f;
            BacEntryInstance.ResetState();

            //Replay ticks the same way live playback runs them, so seeking to a frame gives the same state as playing to it.
            bool reachedFrame = false;

            for (int tick = 0; tick < MaxSeekTicks && !reachedFrame; tick++)
            {
                bool bacRan = SimulateBacTick(false);
                float framesLeft = frame - BacEntryInstance.CurrentFrame;
                reachedFrame = bacRan && framesLeft <= 0f;

                //Fully update anim positions for as long as required for accurate blending
                SimulateScene(framesLeft < numBlendingFrames, framesLeft < 10, framesLeft <= 1, reachedFrame);
            }

            if (!reachedFrame)
                Log.Add($"BacPlayer.Seek: frame {frame} was not reached after {MaxSeekTicks} ticks. The BAC frame is stuck at {BacEntryInstance.CurrentFrame}.", LogType.Warning);

            screenEffectEvaluator.Update(BacEntryInstance, CurrentFrame);

            //Reset camera state if no cam anim is active. This allows the bac entry to be paused, then the camera moved, and the scene to resume without the camera resetting to the last simulated camera
            if (Viewport.Instance.Camera.cameraInstance == null && SceneManager.UseCameras)
            {
                Viewport.Instance.Camera.CameraState.SetState(originalCameraState);
            }
        }

        private void SetLoop(int startFrame, int endFrame, int relativeTo, bool allowIncompleteLoop, bool isAnimationLoop = false)
        {
            if (endFrame <= startFrame) return;

            int newStart = startFrame + relativeTo;
            int newEnd = endFrame + relativeTo;
            int animStart = isAnimationLoop ? startFrame : (int)character.AnimationPlayer.PrimaryCurrentFrame;

            if (BacEntryInstance.CurrentLoop == 0)
            {
                BacEntryInstance.LoopEnabled = true;
                BacEntryInstance.LoopStartFrame = newStart;
                BacEntryInstance.LoopEndFrame = newEnd;
                BacEntryInstance.LoopAnimationStartFrame = animStart;
                BacEntryInstance.LoopCameraStartFrame = (int)Viewport.Instance.Camera.CurrentFrame;
                BacEntryInstance.LoopAllowIncomplete = allowIncompleteLoop;
            }
        }

        #region Helpers
        private void RevertCharacterPosition(bool alwaysRevert)
        {
            if (BacEntryInstance == null) return;

            //IMPORTANT: When adding SEEK for future actors (1 and 2), the PRIMARY animation on them must be removed BEFORE their original matrices are restored here
            for(int i = 0; i < 3; i++)
            {
                if(SceneManager.Actors[i] != null)
                {
                    if (BacEntryInstance.IsPreview || alwaysRevert)
                    {
                        SceneManager.Actors[i].BaseTransform = BacEntryInstance.OriginalMatrix[i];
                        SceneManager.Actors[i].ActionMovementTransform = Matrix4x4.Identity;

                        if (SceneManager.Actors[i].AnimationPlayer.PrimaryAnimation != null)
                            SceneManager.Actors[i].AnimationPlayer.PrimaryAnimation.hasMoved = false;
                    }
                }
            }
        }

        public void ResetBacPreviewState()
        {
            Viewport.Instance.Camera.BuiltInCameras.Clear();

            if (!SceneManager.RetainActionMovement)
            {
                RevertCharacterPosition(false);
                character.PartSet.ResetBacPartSetSwap(false);
            }
            else
            {
                character.MergeTransforms();
            }

            //The BAC frame follows the playing animation, so the last entry's animation must not carry into the next one.
            character.AnimationPlayer.ClearCurrentAnimation(true);

            character.Controller.ResetState(SceneManager.RetainActionMovement);
            SceneManager.Actors[1]?.ResetState(SceneManager.RetainActionMovement);

            //Xv2CoreLib.Random.ResetWithCurrentSeed();
            Xv2CoreLib.Random.GenerateNewSeed();
            VfxManager.StopEffects();

            if(BacEntryInstance != null)
            {
                CurrentFrame = 0;
                BacEntryInstance.ResetState();
            }
        }

        public void ClearBacEntry()
        {
            clearing = true;
            character.ResetTransparencyValues();
            character.PartSet.StopScdAnimations();

            if (IsPreview)
            {
                Viewport.Instance.Camera.ClearCameraAnimation();
                Viewport.Instance.Camera.BuiltInCameras.Clear();
            }

            character.AnimationPlayer.SecondaryAnimations.Clear();
            character.CanBeTracked = true;
            BacEntryInstance?.Dispose();
            BacEntryInstance = null;

            clearing = false;
        }

        /// <summary>
        /// Force re-simulate the current BAC entry. This should be called whenever the BAC entry is edited in a way that affects the simulation.
        /// </summary>
        public void ResimulateCurrentEntry()
        {
            //Values in the current BAC have changed and so the current frame must be re-simulated.
            if (BacEntryInstance != null)
            {
                bool wasPlaying = ViewportInstance.IsPlaying;
                ViewportInstance.IsPlaying = false;

                BacEntryInstance.CalculateEntryDuration();
                Seek(BacEntryInstance.CurrentFrame, true);

                ViewportInstance.IsPlaying = wasPlaying;
            }
        }

        #endregion

        #region PlaybackControl
        public void Resume()
        {
            if (BacEntryInstance == null || SceneManager.CurrentSceneState != EditorTabs.Action) return;

            if (BacEntryInstance.CurrentFrame >= CurrentDuration)
                ResetBacPreviewState();
        }

        public void Stop()
        {
            if (BacEntryInstance == null || SceneManager.CurrentSceneState != EditorTabs.Action) return;

            character.AnimationPlayer.ClearCurrentAnimation(true);
            BacEntryInstance.ResetState();
            character.ResetPosition();
            character.PartSet.ResetBacPartSetSwap(false);
            SceneManager.Actors[1]?.ResetState();
            BacEntryInstance.CreateMatrixRestorePoint();
            character.AnimationTimeScale = 1f;
            ViewportInstance.IsPlaying = false;
            Viewport.Instance.Camera.BuiltInCameras.Clear();
        }

        public void SeekPrevFrame()
        {
            if (BacEntryInstance == null || SceneManager.CurrentSceneState != EditorTabs.Action) return;

            if (CurrentFrame != 0)
            {
                Seek(CurrentFrame - 1, true);
            }
            else
            {
                Seek(CurrentDuration, true);
            }
        }

        public void SeekNextFrame()
        {
            if (BacEntryInstance == null || SceneManager.CurrentSceneState != EditorTabs.Action) return;

            if (CurrentFrame < CurrentDuration)
                StepNextFrameInPlace();
            else
                Seek(0, true);
        }

        /// <summary>
        /// Runs live-playback ticks until the whole BAC frame changes.
        /// </summary>
        private void StepNextFrameInPlace()
        {
            if (BacEntryInstance == null)
                return;

            double startFrame = Math.Floor(BacEntryInstance.CurrentFrame);
            bool frameChanged = false;

            for (int tick = 0; tick < MaxStepTicks && !frameChanged; tick++)
            {
                bool bacRan = SimulateBacTick(true);
                frameChanged = bacRan && Math.Floor(BacEntryInstance.CurrentFrame) != startFrame;
                SimulateScene(true, true, true, frameChanged);
            }

            screenEffectEvaluator.Update(BacEntryInstance, CurrentFrame);
        }

        /// <summary>
        /// Runs the BAC part of one paused tick. Returns false while hit stop holds the BAC frame.
        /// </summary>
        private bool SimulateBacTick(bool allowLoop)
        {
            //Live playback counts hit stop down before the BAC update. Actor.Simulate counts it down later in the tick.
            if (character.Controller.FreezeActionFrames > 1)
                return false;

            BacEntryInstance.AdvanceFrame();
            RunCurrentFrame(allowLoop, character.ActiveTimeScale, true);
            return true;
        }

        private void SimulateScene(bool fullCharacterUpdate, bool fullVictimUpdate, bool fullCameraUpdate, bool forceEffectUpdate)
        {
            character.Simulate(fullCharacterUpdate, true, true);
            SceneManager.Actors[1]?.Simulate(fullVictimUpdate, true, true);
            Viewport.Instance.Camera.Simulate(fullCameraUpdate, true, true);
            ViewportInstance.Simulation.Simulate();

            VfxManager.ForceEffectUpdate = forceEffectUpdate;
            VfxManager.Simulate();
        }
        #endregion

    }
}
