using Microsoft.Xna.Framework;
using System;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC.Simulation;
using XenoKit.Engine.View;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;
using SimdVector4 = System.Numerics.Vector4;

namespace XenoKit.Engine.Scripting.BAC
{
    public partial class BacPlayer
    {
        //Flag 0x1 limits a row to CaCs and 0x2 to roster characters. 0x1 is checked first, so a value of 3 acts like 1.
        private static bool PassesCharacterFlags(ushort flags, bool isCaC)
        {
            if ((flags & 0x1) != 0) return isCaC;
            if ((flags & 0x2) != 0) return !isCaC;
            return true;
        }

        private void UpdateBac(float frameStep, bool isTick)
        {
            if (clearing) return;
            bool isCaC = character.CharacterData.IsCaC;
            if (BacEntryInstance.IsFinished && BacEntryInstance.PreviousFrame >= 0f)
            {
                //DYT and material values last until the entry ends
                character.ResetBacMaterialValues();
                if (isTick && BacEntryInstance.PreviousFrame < BacEntryInstance.Duration)
                    foreach (BAC_Type2 movement in BacEntryInstance.BacEntry.IBacTypes.OfType<BAC_Type2>())
                        if (PassesCharacterFlags(movement.Flags, isCaC)) ApplyMovement(movement);
                return;
            }

            character.SetBacTimeScale(1f, true);
            IOrderedEnumerable<IBacType> validEntries = BacEntryInstance.BacEntry.IBacTypes.Where
                (row => (row is BAC_Type2
                    ? BacTiming.GetCoveredFrames(row.StartTime, row.Duration, BacEntryInstance.PreviousFrame, BacEntryInstance.CurrentFrame) > 0f
                    : row is BAC_Type28 || row is BAC_Type29 ? IsRowRunning(row) : IsValidBacTypeTime(row)) && PassesCharacterFlags(row.Flags, isCaC))
                .OrderBy(x => x.GetType() == typeof(BAC_Type4)); //TimeScale must be resolved before animation/camera
            var settings = SettingsManager.Instance.Settings;

            //Read bac types
            foreach (IBacType type in validEntries)
            {
                //Animation
                if (type is BAC_Type0 animation)
                {
                    if (!BacEntryInstance.TryActivate(type)) continue;
                    if (BAC_Type0.IsFullBodyAnimation(animation.EanType) && animation.StartTime < BacEntryInstance.LoopStartFrame && BacEntryInstance.GetActivationCount(type) > 0) continue; //Animations have special handling in loops

                    Actor animationActor = BacEntryInstance.GetAnimationActor(animation.EanType, BacEntryInstance.User, character);
                    EAN_File eanFile = Files.Instance.GetEanFile(animation.EanType, BacEntryInstance.SkillMove, animationActor, true, true);

                    if (eanFile != null && animation.EanIndex != ushort.MaxValue)
                    {
                        switch (animation.EanType)
                        {
                            case BAC_Type0.EanTypeEnum.Character:
                            case BAC_Type0.EanTypeEnum.Common:
                            case BAC_Type0.EanTypeEnum.MCM_DBA:
                            case BAC_Type0.EanTypeEnum.MCM_TTL:
                            case BAC_Type0.EanTypeEnum.MCM_TU6:
                            case BAC_Type0.EanTypeEnum.MCM_TU13_5:
                            case BAC_Type0.EanTypeEnum.CommonTTL:
                            case BAC_Type0.EanTypeEnum.MCM_XV1:
                            case BAC_Type0.EanTypeEnum.CM2_XVS:
                            case BAC_Type0.EanTypeEnum.MCM_CommonFull:
                            case BAC_Type0.EanTypeEnum.CM2_XAC:
                            case BAC_Type0.EanTypeEnum.CM2_XAN:
                            case BAC_Type0.EanTypeEnum.Skill:
                                character.AnimationPlayer.PlayPrimaryAnimation(eanFile, animation.EanIndex, animation.StartFrame, animation.EndFrame, animation.BlendWeight, animation.BlendWeightFrameStep, animation.AnimFlags, true, animation.TimeScale, false, true);
                                if (animation.StartTime != 0 && character.AnimationPlayer.PrimaryAnimation != null)
                                    character.AnimationPlayer.PrimaryAnimation.SkipToFrame(animation.StartFrame + Math.Max(BacEntryInstance.CurrentFrame - animation.StartTime, 0f));
                                character.AnimationTimeScale = Math.Abs(animation.TimeScale);
                                BacEntryInstance.DrivingAnimation = animation;

                                //Loop frames are animation frames. The BAC frame is StartTime + animation frame - frame offset.
                                SetLoop(animation.LoopStartFrame, character.AnimationPlayer.PrimaryAnimation.EndFrame, animation.StartTime - BacEntryInstance.GetAnimationFrameOffset(BacEntryInstance.BacEntry, animation), true, true);
                                break;
                            case BAC_Type0.EanTypeEnum.FaceBase:
                            case BAC_Type0.EanTypeEnum.FaceForehead:
                                if (ShouldUsePrimaryFaceAnimation(animation, eanFile))
                                {
                                    BacEntryInstance.MainFaceAnimationEndTime = animation.StartTime + animation.Duration;
                                }
                                else
                                {
                                    character.AnimationPlayer.PlaySecondaryAnimation(eanFile, animation.EanIndex, animation.StartFrame, (ushort)(animation.Duration + 1), animation.BlendWeight, animation.BlendWeightFrameStep, animation.TimeScale, true);
                                }

                                break;
                            case BAC_Type0.EanTypeEnum.CommonTail:
                            case BAC_Type0.EanTypeEnum.MCM_LeftHand:
                            case BAC_Type0.EanTypeEnum.MCM_BothHands:
                                character.AnimationPlayer.PlaySecondaryAnimation(eanFile, animation.EanIndex, animation.StartFrame, animation.EndFrame, animation.BlendWeight, animation.BlendWeightFrameStep, animation.TimeScale);
                                break;
                        }
                    }
                }

                //Hitbox
                if (type is BAC_Type1 hitbox)
                {
                    var impactType = hitbox.GetImpactType();
                    //var damageType = hitbox.GetDamageType();

                    //Every impact type except Single spawns again each frame of its window.
                    if (!BacEntryInstance.TryActivate(type) && impactType == BAC_Type1.HitboxFlagsEnum.ImpactType_Single) continue;

                    Actor spawnActor = Collision.BacHitbox.GetSpawnActor(hitbox, BacEntryInstance.User);

                    BacEntryInstance.AddVisualObject(hitbox, ViewportInstance);

                    //Flag 0x8 makes this a break-collision record, not a damaging hitbox.
                    if ((hitbox.HitboxFlags & BAC_Type1.HitboxFlagsEnum.Unk4) != 0) continue;

                    if (ViewportInstance.Simulation.ActiveHitboxes.FirstOrDefault(x => x.Hitbox == hitbox && x.BacEntry == BacEntryInstance) == null)
                    {
                        ViewportInstance.Simulation.ActiveHitboxes.Add(new Collision.BacHitbox(BacEntryInstance, hitbox, spawnActor, BacEntryInstance.User, BacEntryInstance.User.Team));
                    }
                }

                if (type is BAC_Type2 movement)
                {
                    if (isTick) ApplyMovement(movement);
                }

                if (type is BAC_Type5 tracking)
                    ApplyTracking(tracking, isTick);

                //TimeScale
                if (type is BAC_Type4 timeScale)
                {
                    character.SetBacTimeScale(Math.Abs(timeScale.TimeScale), false);
                }

                //Effect
                if (type is BAC_Type8 effect)
                {
                    if ((effect.EffectFlags & BAC_Type8.EffectFlagsEnum.Loop) != BAC_Type8.EffectFlagsEnum.Loop && BacEntryInstance.GetActivationCount(type) > 0) continue;
                    if (!BacEntryInstance.TryActivate(type) ||
                        !settings.XenoKit_VfxSimulation ||
                        (effect.EepkType == BAC_Type8.EepkTypeEnum.StageBG && !settings.XenoKit_StageBgVfxSimulation)) continue;

                    if ((effect.EffectFlags & BAC_Type8.EffectFlagsEnum.Off) == BAC_Type8.EffectFlagsEnum.Off)
                    {
                        VfxManager.StopEffect(effect, BacEntryInstance);
                    }
                    else
                    {
                        VfxManager.PlayEffect(effect, BacEntryInstance, character);
                    }
                }

                //Aura
                if (type is BAC_Type19 aura)
                {
                    if (!aura.AuraFlags.HasFlag(BAC_Type19.AuraFlagsEnum.AllowLoop) && BacEntryInstance.GetActivationCount(type) > 0) continue;
                    if (!BacEntryInstance.TryActivate(type) || !settings.XenoKit_VfxSimulation) continue;

                    VfxManager.PlayAura(aura, character);
                }

                //Screen effect
                if (type is BAC_Type16 screenEffect)
                {
                    bool allowLoop = (screenEffect.ScreenEffectFlags & BAC_Type16.ScreenEffectFlagsEnum.AllowLoop) == BAC_Type16.ScreenEffectFlagsEnum.AllowLoop;

                    if (!allowLoop && BacEntryInstance.GetActivationCount(type) > 0) continue;
                    if (!BacEntryInstance.TryActivate(type)) continue;

                    if ((screenEffect.ScreenEffectFlags & BAC_Type16.ScreenEffectFlagsEnum.DisableEffect) == BAC_Type16.ScreenEffectFlagsEnum.DisableEffect)
                    {
                        BacEntryInstance.ClearScreenEffect(screenEffect);
                        continue;
                    }

                    Xv2CoreLib.BPE.BPE_Entry bpeEntry = Files.Instance.GetBpeEntry(screenEffect.BpeIndex, true);
                    if (screenEffectEvaluator.HasData(bpeEntry))
                        BacEntryInstance.StartScreenEffect(bpeEntry, screenEffect);
                }

                //Projectile
                if (type is BAC_Type9 projectile)
                {
                    bool canLoop = (projectile.BsaFlags & BAC_Type9.BsaFlagsEnum.Loop) == BAC_Type9.BsaFlagsEnum.Loop ||
                                   (projectile.BsaFlags & BAC_Type9.BsaFlagsEnum.AllowLoop) == BAC_Type9.BsaFlagsEnum.AllowLoop;

                    if (!canLoop && BacEntryInstance.GetActivationCount(type) > 0) continue;
                    if (!BacEntryInstance.TryActivate(type) || !settings.XenoKit_ProjectileSimulation) continue;

                    BSA_File bsaFile = Files.Instance.GetBsaFile(projectile.BsaType, projectile.SkillID, BacEntryInstance.SkillMove, BacEntryInstance.User, true);
                    BSA_Entry bsaEntry = bsaFile?.BSA_Entries?.FirstOrDefault(entry => entry.SortID == projectile.EntryID);

                    if (bsaEntry != null)
                    {
                        //Only init when missing. Rebuilding discards unsaved edits, since IBsaTypes is derived from Type0-Type14.
                        if (bsaEntry.IBsaTypes == null)
                            bsaEntry.InitializeIBsaTypes();
                        BacEntryInstance.AddProjectile(projectile, bsaEntry, bsaFile, canLoop);
                    }
                    else
                    {
                        Log.Add($"BacPlayer: Could not find BSA entry {projectile.EntryID} in {projectile.BsaType}.", LogType.Warning);
                    }
                }

                //Camera
                if (type is BAC_Type10 camera)
                {
                    if (!BacEntryInstance.TryActivate(type) || character.ActorSlot != 0) continue;

                    EAN_File ean = Files.Instance.GetCamEanFile(camera.EanType, BacEntryInstance.SkillMove, BacEntryInstance.User, true, true);

                    if (ean == null && SceneManager.UseCameras && Viewport.Instance.Camera.CanRunBuiltInCameras)
                    {
                        Viewport.Instance.Camera.BuiltInCameras.Start(camera, Viewport.Instance.Camera.CameraState, character, BuiltInCameras.GetOpponent());
                    }
                    else if (ean != null && camera.EanIndex != ushort.MaxValue && SceneManager.UseCameras)
                    {
                        EAN_Animation cam = ean.GetAnimation(camera.EanIndex, true);

                        if (cam != null)
                        {
                            int actorFocusIdx = camera.FocusOnTarget ? 1 : character.ActorSlot;

                            SceneManager.PlayCameraAnimation(ean, cam, camera, character, actorFocusIdx, true);
                        }
                        else
                        {
                            Log.Add(string.Format("BacPlayer: Could not find the camera animation with the ID {0} in the {1} cam.ean.", camera.EanIndex, camera.EanType), LogType.Error);
                        }
                    }
                }

                //Sound
                if (type is BAC_Type11 sound)
                {
                    if (!BacEntryInstance.TryActivate(type)) continue;

                    Xv2CoreLib.ACB.ACB_Wrapper acb = Files.Instance.GetAcbFile(sound.AcbType, BacEntryInstance.SkillMove, character, true);

                    //I've made it only play sounds if a primary animation is current playing - this prevents some audio crashes/errors
                    if (acb != null && sound.CueId != ushort.MaxValue && ViewportInstance.IsPlaying && character.AnimationPlayer.PrimaryAnimation != null)
                    {
                        Viewport.Instance.AudioEngine.PlayCue(sound.CueId, acb, character, BacEntryInstance, (sound.SoundFlags & SoundFlags.StopWhenParentEnds) == SoundFlags.StopWhenParentEnds);
                    }
                }

                if (type is BAC_Type18 physicsPartControl && ((ushort)physicsPartControl.Function & 0xff) == 2)
                {
                    if (!BacEntryInstance.TryActivate(type)) continue;
                    character.PartSet.ControlScdAnimation(physicsPartControl);
                }

                //Functions
                if (type is BAC_Type15 function)
                {
                    switch (function.FunctionType)
                    {
                        case 0x0: //BAC Loop. When conditions are no longer true, the loop will stop when the current loop cycle ends.
                        case 0x22: //BAC Loop, but ends instantly when conditions are no longer true
                            if (!BacEntryInstance.TryActivate(type) || BacEntryInstance.CurrentLoop > 0) continue;
                            SetLoop(type.StartTime, type.StartTime + type.Duration, 0, function.FunctionType == 0x22);
                            break;
                        case 0x13: //Sets BCS PartSet (temp).
                        case 0x14: //Sets BCS PartSet (permanent).
                            if (!BacEntryInstance.TryActivate(type)) continue;
                            character.PartSet.ApplyBacPartSetSwap((int)function.Param1, function.FunctionType == 0x14);
                            break;
                        case 0x10: //Pass BSA entry when a projectile BSA Type0 row has the matching BAC condition.
                        case 0x26:
                            BacEntryInstance.AddActiveBsaPassCondition(function.Param1);
                            break;
                        case 0x6: //Invisibility
                            character.IsVisible = false;
                            break;
                        case 0x17:
                            character.CanBeTracked = false;
                            break;
                        case 0x30: //Black Void
                            ViewportInstance.IsBlackVoid = true;
                            break;
                    }
                }

                if (type is BAC_Type28 dytBlend && TryGetRowProgress(type, out float dytProgress))
                {
                    bool dytEnded = dytProgress >= 1f;
                    float ratio = dytEnded ? dytBlend.EndBlendRatio : dytBlend.StartBlendRatio + (dytBlend.EndBlendRatio - dytBlend.StartBlendRatio) * dytProgress;
                    int mainCluster = dytEnded ? dytBlend.EndMainCluster : dytBlend.StartMainCluster;
                    int blendCluster = dytEnded ? dytBlend.EndBlendCluster : dytBlend.StartBlendCluster;
                    character.BacDytMain = mainCluster;
                    character.BacDytBlend = blendCluster;
                    character.BacDytRatio = ratio;
                }

                if (type is BAC_Type29 materialParameter && materialParameter.ShaderParamId == MatScale0ParamId &&
                    materialParameter.ModelIndex < PartTypeCount && TryGetRowProgress(type, out float materialProgress))
                {
                    character.BacMatScale0[(materialParameter.ModelIndex, materialParameter.MaterialIndex)] = new[]
                    {
                        materialParameter.StartValue1 + (materialParameter.EndValue1 - materialParameter.StartValue1) * materialProgress,
                        materialParameter.StartValue2 + (materialParameter.EndValue2 - materialParameter.StartValue2) * materialProgress,
                        materialParameter.StartValue3 + (materialParameter.EndValue3 - materialParameter.StartValue3) * materialProgress,
                        materialParameter.StartValue4 + (materialParameter.EndValue4 - materialParameter.StartValue4) * materialProgress
                    };
                }

                //Eye Movement
                if (type is BAC_Type21 eyeMovement)
                {
                    if (BacEntryInstance.CurrentLoop > 0 && !eyeMovement.AllowLoop) continue;
                    if (!BacEntryInstance.TryActivate(type)) continue;

                    BacEntryInstance.ActiveEyeMovement = eyeMovement;
                }

                if (type is BAC_Type23 transparency)
                {
                    if (!BacEntryInstance.TryActivate(type) && !isTick) continue;

                    float blend = 1f / Math.Max(type.StartTime + type.Duration - BacEntryInstance.CurrentFrame, 1f);
                    character.ShaderParameters.g_vColor4_PS = SimdVector4.Lerp(character.ShaderParameters.g_vColor4_PS,
                        new SimdVector4(transparency.Tint_R, transparency.Tint_G, transparency.Tint_B, transparency.Tint_A), blend);
                    character.ShaderParameters.g_vParam8_PS = SimdVector4.Lerp(character.ShaderParameters.g_vParam8_PS,
                        new SimdVector4(transparency.Desaturation, transparency.RimTintStrength, transparency.RimTintPower, 1f), blend);
                    SimdVector4 lines = SimdVector4.Lerp(character.ShaderParameters.g_vParam9_PS,
                        new SimdVector4(transparency.HorizontalLineSize, transparency.VerticalLineSize, transparency.HorizontalLineSpacing, transparency.VerticalLineSpacing), blend);
                    character.ShaderParameters.g_vParam9_PS = new SimdVector4(
                        (float)Math.Floor(lines.X), (float)Math.Floor(lines.Y), (float)Math.Floor(lines.Z), (float)Math.Floor(lines.W));
                    SimdVector4 pattern = SimdVector4.Lerp(character.ShaderParameters.g_vParam10_PS,
                        new SimdVector4(transparency.HorizontalPatternShift, transparency.VerticalPatternShift, transparency.StripeHeight, transparency.StripeBrightness), blend);
                    character.ShaderParameters.g_vParam10_PS = new SimdVector4(
                        (float)Math.Floor(pattern.X), (float)Math.Floor(pattern.Y), pattern.Z, pattern.W);

                    switch (transparency.DrawType)
                    {
                        case BAC_Type23.DrawTypeEnum.Default:
                            character.ShaderParameters.ShaderPath = Shader.ActorShaderPath.Default;
                            break;
                        case BAC_Type23.DrawTypeEnum.Vanish:
                        case BAC_Type23.DrawTypeEnum.Vanish2:
                        case BAC_Type23.DrawTypeEnum.Vanish3:
                        case BAC_Type23.DrawTypeEnum.Unk4:
                            character.ShaderParameters.ShaderPath = Shader.ActorShaderPath.Vanish;
                            float shaderFlags = transparency.DrawType == BAC_Type23.DrawTypeEnum.Vanish2 ? 115f :
                                transparency.DrawType == BAC_Type23.DrawTypeEnum.Vanish3 ? 243f : 104f;
                            character.ShaderParameters.g_vUserFlag1_VS = new SimdVector4(shaderFlags, 0f, 0f, 0f);
                            break;
                        case BAC_Type23.DrawTypeEnum.Figure:
                            character.ShaderParameters.ShaderPath = Shader.ActorShaderPath.HC;
                            break;
                    }
                }
            }

            screenEffectEvaluator.Update(BacEntryInstance, CurrentFrame);

            //Update Eye Movements
            if (BacEntryInstance.ActiveEyeMovement != null)
            {
                BAC_Type21 eyeMovement = BacEntryInstance.ActiveEyeMovement;

                float elapsedFrames = CurrentFrame - eyeMovement.StartTime;
                if (elapsedFrames > 0 && elapsedFrames >= eyeMovement.EyeMovementDuration)
                {
                    BacEntryInstance.ActiveEyeMovement = null;
                }
                else
                {
                    Vector4 position = EyeMovementPositions.GetPosition(eyeMovement, elapsedFrames, character.PartSet.GetEyeMovementScale());
                    character.EyeIrisLeft_UV[0] = position.X;
                    character.EyeIrisLeft_UV[1] = position.Y;
                    character.EyeIrisRight_UV[0] = position.Z;
                    character.EyeIrisRight_UV[1] = position.W;
                    character.BacEyeMovementUsed = true;
                }

            }

            //Update face animation
            if (character.AnimationPlayer.PrimaryAnimation != null)
                character.AnimationPlayer.PrimaryAnimation.EnableFaceBones = BacEntryInstance.MainFaceAnimationEndTime > CurrentFrame;
        }

        //Modes 1 (actor space) and 2 (world space) only. Drag is the velocity change per frame.
        private void ApplyMovement(BAC_Type2 movement)
        {
            int mode = movement.MovementFlags & 0xF;
            float startFrame = Math.Max(movement.StartTime, BacEntryInstance.PlaybackStartFrame);
            float endFrame = BacEntryInstance.IsFinished ? Math.Min(BacEntryInstance.CurrentFrame, BacEntryInstance.Duration) : BacEntryInstance.CurrentFrame;
            float frameStep = BacTiming.GetCoveredFrames(startFrame, movement.StartTime + movement.Duration - startFrame, BacEntryInstance.PreviousFrame, endFrame);
            if (!SimulateMovement || frameStep <= 0f || (mode != 1 && mode != 2)) return;

            float framesSinceStart = BacEntryInstance.CurrentFrame - startFrame;
            SimdVector3 drag = new SimdVector3(movement.DragX, movement.DragY, movement.DragZ);
            SimdVector3 velocity = new SimdVector3(movement.DirectionX, movement.DirectionY, movement.DirectionZ) + drag * framesSinceStart;
            SimdVector3 displacement = velocity * frameStep;

            //Actor space is the actor's own rotation. Animation root rotation is not part of it.
            if (mode == 1)
                displacement = SimdVector3.TransformNormal(displacement, character.BaseTransform);

            character.ApplyTranslation(displacement);
        }

        private const float MaxTrackingPitch = 80f * (float)Math.PI / 180f;
        private const float AirborneHeight = 0.01f;

        //Turns the actor toward the opponent by the Tracking factor once per tick, not scaled by time.
        //Yaw always turns. Pitch only turns while the actor is above the ground.
        private void ApplyTracking(BAC_Type5 tracking, bool isTick)
        {
            if (!SimulateTracking || !isTick || tracking.Tracking <= 0f) return;

            //Low byte 1 aims at the target actor and 0 at the stored aim point. Both are the opponent here.
            Actor target = character.Opponent;
            if (target == null || !target.CanBeTracked) return;

            SimdVector3 toTarget = target.Transform.Translation - character.Transform.Translation;
            float flatDistanceSquared = toTarget.X * toTarget.X + toTarget.Z * toTarget.Z;

            //Right above or below the target, the game aims from an older position. Holding the facing is close to that.
            if (flatDistanceSquared < 1e-6f || (Math.Abs(toTarget.Y) > 1f && flatDistanceSquared < 1f)) return;

            //Actors face -Z. The body transform includes the animation's root rotation, so the visible facing ends on the target.
            Matrix4x4 bodyTransform = character.ActionMovementTransform * character.BaseTransform;
            SimdVector3 forward = SimdVector3.Normalize(SimdVector3.TransformNormal(-SimdVector3.UnitZ, bodyTransform));
            SimdVector3 direction = SimdVector3.Normalize(toTarget);

            if ((tracking.TrackingFlags & BAC_Type5.TrackingFlagsEnum.NoYaw) == 0)
            {
                SimdVector3 yawDirection = (tracking.TrackingFlags & BAC_Type5.TrackingFlagsEnum.ReverseYaw) != 0 ? -direction : direction;
                float yaw = (float)Math.Atan2(forward.X, forward.Z);
                float targetYaw = (float)Math.Atan2(yawDirection.X, yawDirection.Z);
                float turn = MathHelper.WrapAngle(targetYaw - yaw) * tracking.Tracking;
                character.BaseTransform *= Matrix4x4.CreateRotationY(turn, bodyTransform.Translation);
                bodyTransform = character.ActionMovementTransform * character.BaseTransform;
                forward = SimdVector3.Normalize(SimdVector3.TransformNormal(-SimdVector3.UnitZ, bodyTransform));
            }

            if (!IsPitchTrackingActive()) return;

            float targetPitch = (float)Math.Asin(MathHelper.Clamp(direction.Y, -1f, 1f));
            if ((tracking.TrackingFlags & BAC_Type5.TrackingFlagsEnum.ReversePitch) != 0)
                targetPitch = -targetPitch;

            targetPitch = MathHelper.Clamp(targetPitch, -MaxTrackingPitch, MaxTrackingPitch);
            float pitch = (float)Math.Asin(MathHelper.Clamp(forward.Y, -1f, 1f));
            float pitchTurn = (targetPitch - pitch) * tracking.Tracking;
            SimdVector3 right = SimdVector3.Cross(forward, SimdVector3.UnitY);

            if (right.LengthSquared() < 1e-6f) return;

            right = SimdVector3.Normalize(right);
            Matrix4x4 pitchRotation = Matrix4x4.CreateTranslation(-bodyTransform.Translation) *
                Matrix4x4.CreateFromAxisAngle(right, pitchTurn) * Matrix4x4.CreateTranslation(bodyTransform.Translation);
            character.BaseTransform *= pitchRotation;
        }

        //Pitch turns while a Tracking Axis row with axis bit 0x2 is active, or while the actor is above the ground.
        private bool IsPitchTrackingActive()
        {
            if (character.Transform.Translation.Y > AirborneHeight) return true;

            foreach (IBacType type in BacEntryInstance.BacEntry.IBacTypes)
            {
                if (type is BAC_Type12 targeting && ((ushort)targeting.Axis & 0x2) != 0 && IsValidBacTypeTime(type))
                    return true;
            }

            return false;
        }

        //Shader parameter id of MatScale0, the only one used by Material Parameter rows in the game files
        private const int MatScale0ParamId = 115;
        //Model indices below this are BCS part types. Higher ones are attachments, which the preview does not have.
        private const int PartTypeCount = 10;

        /// <summary>
        /// Progress through a row from 0 to 1. False before the row starts, and after the first update past its end, so the end values are set once.
        /// </summary>
        private bool TryGetRowProgress(IBacType row, out float progress)
        {
            progress = 0f;
            if (!IsRowRunning(row)) return false;

            progress = Math.Min((CurrentFrame - row.StartTime) / (float)row.Duration, 1f);
            return true;
        }

        private bool IsRowRunning(IBacType row)
        {
            return row.Duration > 0 && CurrentFrame >= row.StartTime && BacEntryInstance.PreviousFrame < row.StartTime + row.Duration;
        }

        private bool ShouldUsePrimaryFaceAnimation(BAC_Type0 animation, EAN_File eanFile)
        {
            return animation.StopFaceLayer == 1 || eanFile?.IsCharaUnique == true;
        }

        private bool IsValidBacTypeTime(IBacType type)
        {
            return BacEntryInstance.IsValidTime(GetSimulationStartFrame(type), type.Duration);
        }

        private ushort GetSimulationStartFrame(IBacType type)
        {
            if (!ShouldDelaySimulationActivation(type))
                return type.StartTime;

            return (ushort)Math.Min(type.StartTime + 1, ushort.MaxValue);
        }

        private bool ShouldDelaySimulationActivation(IBacType type)
        {
            if (!BacEntryInstance.IsPreview)
                return false;

            return type is BAC_Type8 || type is BAC_Type9 || type is BAC_Type11;
        }
    }
}
