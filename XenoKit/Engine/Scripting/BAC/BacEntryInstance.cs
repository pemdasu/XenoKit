using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Scripting.BSA;
using XenoKit.Engine.Scripting.BAC.Simulation;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BPE;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector4 = System.Numerics.Vector4;

namespace XenoKit.Engine.Scripting.BAC
{
    public class BacEntryInstance : ScriptEntity
    {
        //Files:
        /// <summary>
        /// The user of this BAC entry. Required for retrieving the Character EAN/CAM/BDM/ACBs. (The character the BAC entry is playing on may differ from the User, such as in Throw moves)
        /// </summary>
        public Actor User;
        /// <summary>
        /// The actor this BAC entry is playing on.
        /// </summary>
        public Actor Actor;
        /// <summary>
        /// The Move that this BAC entry belongs to. Required for retrieving skill EAN/CAM/BDM/BSA/ACBs. (Not required for Character/CMN)
        /// </summary>
        public Move SkillMove;
        /// <summary>
        /// The BAC_File that this entry belongs to. Required for Functions: GoTo BAC Entry types (0x1d, 0x25) and ThrowHandler.
        /// </summary>
        public BAC_File BacFile;
        public BAC_Entry BacEntry;

        //State:
        public float PreviousFrame = BacTiming.NotStartedFrame;
        public float CurrentFrame = BacTiming.NotStartedFrame;
        /// <summary>
        /// The Type0 entry whose animation drives the BAC frame.
        /// </summary>
        public BAC_Type0 DrivingAnimation;
        public int PlaybackStartFrame { get; private set; }
        public int Duration { get; private set; }
        public float ScaledDuration { get; private set; }
        public bool HasTimeScale { get; private set; }
        public bool IsPreview { get; private set; }
        private readonly Dictionary<IBacType, int> activationCounts = new Dictionary<IBacType, int>();

        internal int GetActivationCount(IBacType row)
        {
            return activationCounts.TryGetValue(row, out int count) ? count : 0;
        }

        internal bool TryActivate(IBacType row)
        {
            int count = GetActivationCount(row);
            if (count > CurrentLoop) return false;
            activationCounts[row] = count + 1;
            return true;
        }

        //Loop:
        public int CurrentLoop { get; set; } //0 = Initial, 1+ loops
        public bool LoopEnabled = false;
        public bool LoopIsDirty = false;
        public int LoopStartFrame = 0;
        public int LoopEndFrame = 0;
        public int LoopAnimationStartFrame = 0;
        public int LoopCameraStartFrame = 0;
        public bool LoopAllowIncomplete = true;

        //Position:
        /// <summary>
        /// The character position matrix when the BAC entry started.
        /// </summary>
        public Matrix4x4[] OriginalMatrix;

        //Visual objects that render on screen as the entry plays. Currently only covers hitbox previews.
        //These have no actual logic and dont interact with anything. They are purely visual helpers.
        public List<BacVisualCueObject> VisualSimulationCues;
        public readonly List<ProjectileInstance> Projectiles = new List<ProjectileInstance>();
        private readonly HashSet<BAC_Type9> spawnedProjectileTypes = new HashSet<BAC_Type9>();
        private readonly Dictionary<BAC_Type9, float> loopProjectileSpawnFrames = new Dictionary<BAC_Type9, float>();
        private readonly List<float> activeBsaPassConditions = new List<float>();
        private const float BsaConditionTolerance = 0.001f;

        private readonly Dictionary<ushort, BacScreenEffectInstance> activeScreenEffects = new Dictionary<ushort, BacScreenEffectInstance>();
        private readonly Action<ushort> clearScreenEffectCallback;
        private readonly Action clearBodyOutlineCallback;
        internal IEnumerable<BacScreenEffectInstance> ActiveScreenEffects => activeScreenEffects.Values;
        internal Action<ushort> ClearScreenEffectCallback => clearScreenEffectCallback;
        internal Action ClearBodyOutlineCallback => clearBodyOutlineCallback;
        public BacScreenEffectState ScreenEffectState { get; } = new BacScreenEffectState();

        /// <summary>
        /// The currently active eye movement entry. Only one of these can be active at any given time.
        /// </summary>
        public BAC_Type21 ActiveEyeMovement = null;
        /// <summary>
        /// Acts as the duration of how long the face animation in the currently active main animation can be played.
        /// </summary>
        public int MainFaceAnimationEndTime = -1;

        public BacEntryInstance(BAC_File bacFile, BAC_Entry bacEntry, Move currentMove, Actor user, Actor actor, bool isPreview)
        {
            clearScreenEffectCallback = ClearScreenEffect;
            clearBodyOutlineCallback = ClearBodyOutlineValues;
            BacFile = bacFile;
            BacEntry = bacEntry;
            SkillMove = currentMove;
            User = user;
            Actor = actor;
            CalculateEntryDuration();

            IsPreview = isPreview;

            if (IsPreview)
            {
                VisualSimulationCues = new List<BacVisualCueObject>(16);
                OriginalMatrix = new Matrix4x4[3];
                CreateMatrixRestorePoint();
            }
            else
            {
                VisualSimulationCues = null;
                OriginalMatrix = null;
            }

            if (BacEntry == null)
                Log.Add($"Bac Entry {bacEntry} not found!", LogType.Error);
        }

        public void CreateMatrixRestorePoint()
        {
            for (int i = 0; i < 3; i++)
            {
                OriginalMatrix[i] = SceneManager.Actors[i] != null ? SceneManager.Actors[i].ActionMovementTransform * SceneManager.Actors[i].BaseTransform : Matrix4x4.Identity;
            }
        }

        #region AnimationLengthCalculation
        public void CalculateEntryDuration()
        {
            if (BacEntry == null) return;

            PlaybackStartFrame = BacEntry.IBacTypes.OfType<BAC_Type0>()
                .FirstOrDefault(animation => BAC_Type0.IsFullBodyAnimation(animation.EanType))?.StartFrame ?? 0;
            int duration = 0;
            float scaledDuration = 0;
            bool hasTimeScale = false;
            CalculateEntryDuration(BacEntry, SkillMove, User, Actor, true, ref duration, ref scaledDuration, ref hasTimeScale);

            Duration = duration;
            ScaledDuration = scaledDuration;
            HasTimeScale = hasTimeScale;
        }

        public static Actor GetAnimationActor(BAC_Type0.EanTypeEnum source, Actor user, Actor actor)
        {
            return source == BAC_Type0.EanTypeEnum.FaceBase || source == BAC_Type0.EanTypeEnum.FaceForehead
                || source == BAC_Type0.EanTypeEnum.CommonTTL || source == BAC_Type0.EanTypeEnum.CM2_XVS ? actor : user;
        }

        public static int CalculateEntryDuration(BAC_Entry bacEntry, Move files, Actor user)
        {
            int duration = 0;
            float scaled = 0;
            bool hasScale = false;
            CalculateEntryDuration(bacEntry, files, user, user, false, ref duration, ref scaled, ref hasScale);

            return duration;
        }

        public static void CalculateEntryDuration(BAC_Entry bacEntry, Move files, Actor user, Actor actor, bool calculateScaledDuration, ref int duration, ref float scaledDuration, ref bool hasTimeScale)
        {
            int currentStartTime = 0;
            int currentAnimDuration = 0;

            //Calulcate base duration
            foreach (IBacType type in bacEntry.IBacTypes.OrderBy(x => x.StartTime))
            {
                //Animations
                if (type is BAC_Type0 animation)
                {
                    //Dont count this if its not a full body animation
                    if (!BAC_Type0.IsFullBodyAnimation(animation.EanType))
                        continue;

                    int animTypeDuration = CalculateAnimationDuration(bacEntry, files, GetAnimationActor(animation.EanType, user, actor), animation);

                    currentStartTime = animation.StartTime;
                    currentAnimDuration = animTypeDuration;
                    animation.CachedActualDuration = animTypeDuration;
                }
            }

            //Calculate scaled duration
            if (calculateScaledDuration)
            {
                hasTimeScale = false;
                scaledDuration = currentStartTime + currentAnimDuration;

                foreach (IBacType type in bacEntry.IBacTypes)
                {
                    if (type is BAC_Type0 type0)
                    {
                        //Dont count this if its not a full body animation
                        if (!BAC_Type0.IsFullBodyAnimation(type0.EanType))
                            continue;

                        if (type0.TimeScale != 1f)
                        {
                            scaledDuration -= type0.CachedActualDuration;
                            scaledDuration += type0.CachedActualDuration / type0.TimeScale;
                            hasTimeScale = true;
                        }
                    }

                    if (type is BAC_Type4 timeScale)
                    {
                        scaledDuration -= timeScale.Duration;
                        scaledDuration += timeScale.Duration / timeScale.TimeScale;
                        hasTimeScale = true;
                    }
                }

            }

            duration = currentStartTime + currentAnimDuration;
        }

        /// <summary>
        /// The first full-body Type0 keeps its StartFrame in the BAC clock. Later full-body rows subtract their StartFrame.
        /// </summary>
        public static int GetAnimationFrameOffset(BAC_Entry bacEntry, BAC_Type0 animation)
        {
            foreach (IBacType type in bacEntry.IBacTypes)
            {
                if (type is BAC_Type0 firstAnimation && BAC_Type0.IsFullBodyAnimation(firstAnimation.EanType))
                    return firstAnimation == animation ? 0 : animation.StartFrame;
            }

            return animation.StartFrame;
        }

        /// <summary>
        /// The number of BAC frames from the Type0 StartTime to the end of its animation.
        /// </summary>
        public static int CalculateAnimationDuration(BAC_Entry bacEntry, Move files, Actor user, BAC_Type0 animationBacType)
        {
            int animTypeDuration = 0;
            int frameOffset = GetAnimationFrameOffset(bacEntry, animationBacType);

            //If endFrame is 0xffff, then get duration from the ean
            if (animationBacType.EndFrame != ushort.MaxValue)
            {
                animTypeDuration = animationBacType.EndFrame - frameOffset;
            }
            else
            {
                EAN_File eanFile = Files.Instance.GetEanFile(animationBacType.EanType, files, user, true, true);

                if (eanFile != null)
                {
                    EAN_Animation anim = eanFile.GetAnimation(animationBacType.EanIndex, true);

                    if (anim != null && BAC_Type0.IsFullBodyAnimation(animationBacType.EanType))
                    {
                        animTypeDuration = anim.FrameCount - 1 - frameOffset;
                    }
                }
            }

            return animTypeDuration;
        }

        public static void CalculateLoop(BAC_Entry bacEntry, Move files, Actor user, out int loopStart, out int loopEnd)
        {
            foreach(IBacType type in bacEntry.IBacTypes.Where(x => x.TypeID == 0 || x.TypeID == 15).OrderBy(x => x.StartTime))
            {
                if(type is BAC_Type0 anim)
                {
                    if(anim.LoopStartFrame != ushort.MaxValue && BAC_Type0.IsFullBodyAnimation(anim.EanType))
                    {
                        loopStart = anim.StartTime + anim.LoopStartFrame - GetAnimationFrameOffset(bacEntry, anim);
                        loopEnd = anim.StartTime + CalculateAnimationDuration(bacEntry, files, user, anim);
                        return;
                    }
                }
                else if(type is BAC_Type15 func)
                {
                    if(func.FunctionType == 0 || func.FunctionType == 0x22)
                    {
                        loopStart = type.StartTime;
                        loopEnd = type.StartTime + type.Duration;
                        return;
                    }
                }
            }

            loopStart = -1;
            loopEnd = -1;
        }
        #endregion

        public void Update(float frameStep)
        {
            InScope = CurrentFrame < Duration;

            UpdateVisualObjects();
            UpdateProjectiles(frameStep);
        }

        /// <summary>
        /// Moves to the next BAC frame, which follows the actor's primary animation.
        /// </summary>
        public void AdvanceFrame()
        {
            AnimationInstance animation = Actor.AnimationPlayer.PrimaryAnimation;
            bool hasAnimation = animation != null;
            bool hasDrivingAnimation = DrivingAnimation != null;

            PreviousFrame = CurrentFrame;
            CurrentFrame = BacTiming.GetNextFrame(
                CurrentFrame,
                hasAnimation,
                hasAnimation ? animation.CurrentFrame : 0f,
                hasAnimation && animation.CurrentFrame >= animation.EndFrame,
                hasDrivingAnimation,
                hasDrivingAnimation ? DrivingAnimation.StartTime : 0,
                hasDrivingAnimation ? GetAnimationFrameOffset(BacEntry, DrivingAnimation) : 0,
                PlaybackStartFrame);

            IsFinished = CurrentFrame >= Duration;
        }

        /// <summary>
        /// Checks if a StartTime is valid within the current context.
        /// </summary>
        public bool IsValidTime(ushort startFrame, ushort duration)
        {
            return BacTiming.IsActive(startFrame, duration, PreviousFrame, CurrentFrame);
        }

        public void RefreshDetails()
        {
            CalculateEntryDuration();
        }

        public void ResetState()
        {
            Actor?.ResetTransparencyValues();
            Actor?.ResetBacMaterialValues();
            activationCounts.Clear();
            ClearVisualObjects();
            ClearProjectiles();
            ClearScreenEffect();

            IsFinished = false;
            ActiveEyeMovement = null;
            MainFaceAnimationEndTime = -1;
            CurrentFrame = BacTiming.NotStartedFrame;
            PreviousFrame = BacTiming.NotStartedFrame;
            DrivingAnimation = null;
            CurrentLoop = 0;
            LoopEnabled = false;
            LoopStartFrame = 0;
            LoopEndFrame = 0;
            LoopIsDirty = false;
            ClearActiveBsaPassConditions();
            User?.ResetRandom();

            if (SceneManager.RetainActionMovement && IsPreview)
            {
                SceneManager.Actors[1]?.Controller.ResetState(false);
                CreateMatrixRestorePoint();
            }
        }

        public void Dispose()
        {
            InScope = false;
            ClearVisualObjects();
            ClearProjectiles();
            ClearScreenEffect();
            ClearActiveBsaPassConditions();
        }

        public void StartScreenEffect(BPE_Entry bpeEntry, BAC_Type16 screenEffect)
        {
            ushort bpeIndex = checked((ushort)bpeEntry.SortID);
            activeScreenEffects[bpeIndex] = new BacScreenEffectInstance(
                bpeEntry,
                screenEffect.ScreenEffectFlags,
                (int)CurrentFrame);
        }

        public void StartScreenEffect(BPE_Entry bpeEntry)
        {
            ushort bpeIndex = checked((ushort)bpeEntry.SortID);
            activeScreenEffects[bpeIndex] = new BacScreenEffectInstance(bpeEntry, 0, (int)CurrentFrame);
        }

        public void ClearScreenEffect(BAC_Type16 screenEffect)
        {
            ushort bpeIndex = screenEffect.BpeIndex;
            if (activeScreenEffects.TryGetValue(bpeIndex, out BacScreenEffectInstance activeEffect) &&
                GetScreenEffectMatchFlags(activeEffect.ScreenEffectFlags) == GetScreenEffectMatchFlags(screenEffect.ScreenEffectFlags))
            {
                ClearScreenEffect(bpeIndex);
            }
        }

        public void ClearScreenEffect(ushort bpeIndex)
        {
            activeScreenEffects.Remove(bpeIndex);

            if (activeScreenEffects.Count == 0)
            {
                ScreenEffectState.Clear();
                ClearBodyOutlineValues();
            }
        }

        public void ClearScreenEffect()
        {
            activeScreenEffects.Clear();
            ScreenEffectState.Clear();

            ClearBodyOutlineValues();
        }

        public void ClearBodyOutlineValues()
        {
            if (Actor != null)
                Actor.ClearBodyOutlineValues();
        }

        private static BAC_Type16.ScreenEffectFlagsEnum GetScreenEffectMatchFlags(BAC_Type16.ScreenEffectFlagsEnum flags)
        {
            return flags & ~BAC_Type16.ScreenEffectFlagsEnum.DisableEffect;
        }

        public void AddVisualObject(BAC_Type1 hitbox, Viewport game)
        {
            if (IsPreview)
            {
                if(VisualSimulationCues.FirstOrDefault(x => x.BacType == hitbox) == null)
                {
                    VisualSimulationCues.Add(new HitboxPreview(hitbox, this));
                }
            }
        }

        public void ClearActiveBsaPassConditions()
        {
            activeBsaPassConditions.Clear();
        }

        public void AddActiveBsaPassCondition(float condition)
        {
            if (!activeBsaPassConditions.Any(activeCondition => System.Math.Abs(activeCondition - condition) <= BsaConditionTolerance))
                activeBsaPassConditions.Add(condition);
        }

        public bool HasActiveBsaPassCondition(float condition)
        {
            return activeBsaPassConditions.Any(activeCondition => System.Math.Abs(activeCondition - condition) <= BsaConditionTolerance);
        }

        public bool AddProjectile(BAC_Type9 projectileType, BSA_Entry bsaEntry, BSA_File bsaFile, bool canLoop)
        {
            if (bsaEntry == null) return false;

            if (canLoop)
            {
                if (loopProjectileSpawnFrames.TryGetValue(projectileType, out float lastFrame) && System.Math.Abs(lastFrame - CurrentFrame) < 0.001f)
                    return false;

                loopProjectileSpawnFrames[projectileType] = CurrentFrame;
            }
            else
            {
                if (spawnedProjectileTypes.Contains(projectileType))
                    return false;

                spawnedProjectileTypes.Add(projectileType);
            }

            Projectiles.Add(new ProjectileInstance(this, projectileType, bsaEntry, bsaFile));
            return true;
        }

        private void UpdateVisualObjects()
        {
            if (!IsPreview) return;

            for (int i = VisualSimulationCues.Count - 1; i >= 0; i--)
            {
                if (VisualSimulationCues[i].IsValidForCurrentFrame())
                {
                    VisualSimulationCues[i].Update();
                }
                else
                {
                    VisualSimulationCues.RemoveAt(i);
                }
            }
        }

        private void ClearVisualObjects()
        {
            if (!IsPreview) return;

            VisualSimulationCues.Clear();
        }

        private void UpdateProjectiles(float frameStep)
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                if (Projectiles[i].IsFinished)
                {
                    Projectiles[i].Expire();
                    Projectiles.RemoveAt(i);
                    continue;
                }

                Projectiles[i].Update(frameStep);
            }
        }

        private void ClearProjectiles()
        {
            foreach (ProjectileInstance projectile in Projectiles)
                projectile.Dispose();

            Projectiles.Clear();
            spawnedProjectileTypes.Clear();
            loopProjectileSpawnFrames.Clear();
        }
    }

    internal sealed class BacScreenEffectInstance
    {
        public BPE_Entry Entry { get; private set; }
        public BAC_Type16.ScreenEffectFlagsEnum ScreenEffectFlags { get; private set; }
        public int StartFrame { get; private set; }

        public BacScreenEffectInstance(BPE_Entry entry, BAC_Type16.ScreenEffectFlagsEnum screenEffectFlags, int startFrame)
        {
            Entry = entry;
            ScreenEffectFlags = screenEffectFlags;
            StartFrame = startFrame;
        }
    }

    public sealed class BacScreenEffectState
    {
        public float BlurAmount;
        public float WhiteShineAmount;
        public System.Numerics.Vector4 ColorMultiply = SimdVector4.One;
        public System.Numerics.Vector4 ColorAdd;
        public bool HasColorMultiply;
        public bool HasColorAdd;
        public int HueMode = -1;
        public System.Numerics.Vector4 HueColor;
        public float HueStrength = 1f;
        public bool HasHue;
        //Color filter layers do not combine. The last active one is used.
        public System.Numerics.Matrix4x4 ColorFilter = System.Numerics.Matrix4x4.Identity;
        public bool HasColorFilter;
        public float ZoomLevel;

        public void Clear()
        {
            BlurAmount = 0f;
            WhiteShineAmount = 0f;
            ColorMultiply = SimdVector4.One;
            ColorAdd = SimdVector4.Zero;
            HasColorMultiply = false;
            HasColorAdd = false;
            HueMode = -1;
            HueColor = SimdVector4.Zero;
            HueStrength = 1f;
            HasHue = false;
            ColorFilter = System.Numerics.Matrix4x4.Identity;
            HasColorFilter = false;
            ZoomLevel = 0f;
        }
    }

}
