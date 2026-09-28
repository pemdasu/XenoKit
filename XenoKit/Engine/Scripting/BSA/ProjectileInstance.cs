using System;
using System.Linq;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Vfx;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.BDM;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    public partial class ProjectileInstance : EngineObject, IDisposable
    {
        private const byte SpawnOrientationDefault = 0;
        private const byte SpawnOrientationUserDirection1 = 1;
        internal const byte SpawnOrientationUserDirectionValue = 3;

        private readonly Actor actor;
        private readonly Actor attachActor;
        private readonly Move move;
        private readonly BSA_File bsaFile;
        private readonly BDM_File shotBdm;
        private readonly BSA_Entry bsaEntry;
        private readonly BacEntryInstance bacInstance;
        private readonly BAC_Type9 projectileType;
        private readonly ProjectileInstance parent;
        private readonly BsaPassReason spawnReason;
        private readonly bool allowBacConditionPassEntries;
        private readonly Matrix4x4 initialTransform;
        private readonly bool canFollowAttachTransform;
        private readonly Matrix4x4 initialMotionTransform;
        private readonly Matrix4x4 initialUserDirectionAttachRotation;
        private readonly List<MovementState> movements;
        private readonly List<BSA_Type7> sounds;
        private readonly List<BSA_Type8> screenEffects;
        private readonly List<BSA_Type0> passEntries;
        private readonly HashSet<BSA_Type6> playedEffects = new HashSet<BSA_Type6>();
        private readonly HashSet<BSA_Type7> playedSounds = new HashSet<BSA_Type7>();
        private readonly HashSet<BSA_Type8> playedScreenEffects = new HashSet<BSA_Type8>();
        private readonly List<ActiveProjectileEffect> activeEffects = new List<ActiveProjectileEffect>();
        private readonly List<ProjectileInstance> childProjectiles = new List<ProjectileInstance>();
        private readonly List<BsaHitboxPreview> hitboxPreviews;
        private readonly int expiryFrame;
        private readonly int endFrame;
        private readonly int passDepth;
        private const int MaxBsaPassDepth = 16;
        private bool expiryPassStarted;
        private bool hasHitEnemy;
        private bool entryPassStarted;

        private float currentFrame;
        private Matrix4x4 transform;
        private Matrix4x4 motionTransform;
        private bool isAttachedToSource;
        private bool hasDetachedFromSource;
        private float detachFrame;
        private Matrix4x4 detachWorldTransform;

        public bool IsFinished => (entryPassStarted || currentFrame >= endFrame) && childProjectiles.Count == 0;
        public override Matrix4x4 Transform { get => transform; set => transform = value; }
        public float CurrentFrame => currentFrame;
        public int EndFrame => endFrame;

        public ProjectileInstance(BacEntryInstance bacInstance, BAC_Type9 projectileType, BSA_Entry bsaEntry, BSA_File bsaFile)
            : this(bacInstance, null, bacInstance?.User, GetSpawnActor(bacInstance?.User, projectileType), bacInstance?.SkillMove, bsaFile, bsaEntry, projectileType, CreateSpawnTransform(bacInstance, projectileType), 0, true, BsaPassReason.Root)
        {
            if (projectileType != null && projectileType.SpawnSource >= 4)
                Log.Add($"BAC Type 9 spawn source {projectileType.SpawnSource} is not simulated. The projectile preview uses the user's bone transform.", LogType.Warning);
        }

        public static ProjectileInstance CreatePreview(Actor actor, Move move, BSA_Entry bsaEntry, BSA_File bsaFile, Matrix4x4 spawnTransform)
        {
            return new ProjectileInstance(null, null, actor, actor, move, bsaFile, bsaEntry, null, spawnTransform, 0, false, BsaPassReason.Root);
        }

        private ProjectileInstance(BacEntryInstance bacInstance, ProjectileInstance parent, Actor actor, Actor attachActor, Move move, BSA_File bsaFile, BSA_Entry bsaEntry, BAC_Type9 projectileType, Matrix4x4 spawnTransform, int passDepth, bool allowBacConditionPassEntries, BsaPassReason spawnReason)
        {
            this.bacInstance = bacInstance;
            this.projectileType = projectileType;
            this.parent = parent;
            this.actor = actor;
            this.attachActor = attachActor;
            this.move = move;
            this.bsaFile = bsaFile;
            if (parent != null)
            {
                shotBdm = parent.shotBdm;
            }
            else
            {
                Move commonMove = projectileType == null || projectileType.BsaType == BAC_Type9.BsaTypeEnum.Common
                    ? Files.Instance.GetCmnMove() : null;
                shotBdm = projectileType?.BsaType == BAC_Type9.BsaTypeEnum.Common ||
                    (commonMove?.Files?.BsaFile?.File != null && ReferenceEquals(bsaFile, commonMove.Files.BsaFile.File))
                        ? commonMove?.Files?.ShotBdmFile?.File : move?.Files?.ShotBdmFile?.File;
            }
            this.bsaEntry = bsaEntry;
            this.passDepth = passDepth;
            this.spawnReason = spawnReason;
            this.allowBacConditionPassEntries = allowBacConditionPassEntries;
            canFollowAttachTransform = ShouldFollowLiveAttachTransform(projectileType);
            isAttachedToSource = canFollowAttachTransform;
            initialUserDirectionAttachRotation = GetUserDirectionAttachRotation(attachActor, projectileType);
            movements = bsaEntry.IBsaTypes?
                .OfType<BSA_Type1>()
                .OrderBy(x => x.StartTime)
                .Select(x => new MovementState(x))
                .ToList() ?? new List<MovementState>();
            sounds = bsaEntry.IBsaTypes?.OfType<BSA_Type7>().ToList() ?? new List<BSA_Type7>();
            screenEffects = bsaEntry.IBsaTypes?.OfType<BSA_Type8>().ToList() ?? new List<BSA_Type8>();
            passEntries = bsaEntry.IBsaTypes?.OfType<BSA_Type0>().ToList() ?? new List<BSA_Type0>();
            var hitCounts = new Dictionary<int, int>();
            hitboxPreviews = bsaEntry.IBsaTypes?
                .OfType<BSA_Type3>()
                .Select(x => new BsaHitboxPreview(
                    x,
                    () => BsaHitboxGeometry.UsesDistanceRelativeGeometry(x) ? GetProjectileTransformAtFrame(x.StartTime) : transform,
                    () => transform,
                    () => (int)Math.Floor(currentFrame),
                    hitCounts))
                .ToList() ?? new List<BsaHitboxPreview>();
            motionTransform = canFollowAttachTransform ? CreateProjectileLocalTransform(projectileType) : spawnTransform;
            initialMotionTransform = motionTransform;
            transform = canFollowAttachTransform ? CreateWorldTransformFromMotion(motionTransform) : spawnTransform;
            initialTransform = transform;
            expiryFrame = Math.Max((int)bsaEntry.I_22, 1);
            endFrame = GetEndFrame();
        }

        public void Update(float frameStep, bool playAudio = true)
        {
            if (entryPassStarted)
            {
                currentFrame += frameStep;
                UpdateChildProjectiles(frameStep, playAudio);
                return;
            }

            float previousFrame = currentFrame;
            float targetFrame = currentFrame;

            if (frameStep > 0f)
            {
                targetFrame = currentFrame + frameStep;
                Move(previousFrame, targetFrame);
                currentFrame = targetFrame;
            }

            RefreshWorldTransform();

            PlayDueEffects(previousFrame, currentFrame);
            PlayDueScreenEffects(previousFrame, currentFrame);
            if (frameStep > 0f && playAudio)
                PlayDueSounds(previousFrame, currentFrame);
            PlayDuePassEntries(previousFrame, currentFrame);

            if (frameStep > 0f && !entryPassStarted)
                TryStartPassEntry(BsaPassReason.Expires);

            UpdateActiveEffectTransforms();
            UpdateHitboxes();
            if (frameStep > 0f && !entryPassStarted)
                ApplyHitboxDamage();
            UpdateChildProjectiles(frameStep, playAudio);
        }

        public void Dispose()
        {
            End(true);
        }

        public void Expire()
        {
            End(false);
        }

        private void End(bool force)
        {
            EndEffects(force);
            EndChildProjectiles(force);
            DisposeHitboxes();
            playedEffects.Clear();
            playedSounds.Clear();
            playedScreenEffects.Clear();
        }

        private void EndEffects(bool force)
        {
            foreach (ActiveProjectileEffect effect in activeEffects)
                effect.Effect?.Terminate(force);

            activeEffects.Clear();
        }

        private void EndChildProjectiles(bool force)
        {
            foreach (ProjectileInstance projectile in childProjectiles)
            {
                if (force)
                    projectile.Dispose();
                else
                    projectile.Expire();
            }

            childProjectiles.Clear();
        }

        private void DisposeHitboxes()
        {
            foreach (BsaHitboxPreview hitboxPreview in hitboxPreviews)
                hitboxPreview.Dispose();

            hitboxPreviews.Clear();
        }

        public void Draw()
        {
            if (!entryPassStarted)
            {
                foreach (BsaHitboxPreview hitboxPreview in hitboxPreviews)
                    hitboxPreview.Draw();
            }

            foreach (ProjectileInstance projectile in childProjectiles)
                projectile.Draw();
        }

        private int GetEndFrame()
        {
            int lastFrame = ShouldUseEntryLifetimeForEndFrame() ? expiryFrame : 1;

            foreach (IBsaType type in bsaEntry.IBsaTypes ?? Enumerable.Empty<IBsaType>())
            {
                if (!ShouldCountTypeForEndFrame(type))
                    continue;

                if (type.Duration == 0)
                    continue;

                lastFrame = Math.Max(lastFrame, (int)type.StartTime + type.Duration);
            }

            return lastFrame <= 1 ? expiryFrame : lastFrame;
        }

        private bool ShouldUseEntryLifetimeForEndFrame()
        {
            return parent == null || spawnReason != BsaPassReason.SystemPass;
        }

        private bool ShouldCountTypeForEndFrame(IBsaType type)
        {
            if (type is BSA_Type1)
                return false;

            if (ShouldUseEntryLifetimeForEndFrame())
                return true;

            return type is BSA_Type3 || type is BSA_Type6 || type is BSA_Type7 || type is BSA_Type8;
        }

        private void PlayDueEffects(float previousFrame, float targetFrame)
        {
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation) return;
            if (actor == null) return;

            foreach (BSA_Type6 effect in bsaEntry.IBsaTypes?.OfType<BSA_Type6>() ?? Enumerable.Empty<BSA_Type6>())
            {
                if (playedEffects.Contains(effect) || !IsEffectDue(effect, previousFrame, targetFrame))
                    continue;

                if (((ushort)effect.I_08 & 1) != 0)
                    StopEffect(effect);
                else
                    PlayEffect(effect, GetProjectileTransformAtFrame(effect.StartTime));

                playedEffects.Add(effect);
            }
        }

        private void PlayDueSounds(float previousFrame, float targetFrame)
        {
            foreach (BSA_Type7 sound in sounds)
            {
                if (playedSounds.Contains(sound) || !IsTimedTypeDue(sound, previousFrame, targetFrame))
                    continue;

                playedSounds.Add(sound);

                if (actor == null || Viewport.Instance?.IsPlaying != true || sound.CueId == ushort.MaxValue ||
                    (sound.I_06 & 0x8000) != 0 || (sound.I_02 & 0x2000) != 0)
                    continue;

                if (!BsaSoundResources.TryGetBacAcbType(sound.AcbType, out Xv2CoreLib.BAC.AcbType acbType))
                    continue;

                Xv2CoreLib.ACB.ACB_Wrapper acb = Files.Instance.GetAcbFile(acbType, move, actor, true);
                if (acb != null)
                    Viewport.Instance.AudioEngine.PlayCue(sound.CueId, acb, this);
            }
        }

        private void PlayDueScreenEffects(float previousFrame, float targetFrame)
        {
            foreach (BSA_Type8 screenEffect in screenEffects)
            {
                if (playedScreenEffects.Contains(screenEffect) || !IsTimedTypeDue(screenEffect, previousFrame, targetFrame))
                    continue;

                playedScreenEffects.Add(screenEffect);
                Xv2CoreLib.BPE.BPE_Entry bpeEntry = Files.Instance.GetBpeEntry(screenEffect.I_00, true);
                if (bpeEntry == null)
                    continue;

                if (bacInstance != null)
                    bacInstance.StartScreenEffect(bpeEntry);
                else
                    BsaEffectPreviewController.Instance.StartScreenEffect(bpeEntry);
            }
        }

        private void PlayEffect(BSA_Type6 effect, Matrix4x4 eventProjectileTransform)
        {
            Matrix4x4 offset = CreateEffectOffset(effect);
            Matrix4x4 effectTransform = ApplyEffectOffset(eventProjectileTransform, offset);
            VfxEffect vfxEffect = actor.VfxManager.PlayProjectileEffect(effect, move, actor, effectTransform);

            if (vfxEffect != null)
            {
                activeEffects.Add(new ActiveProjectileEffect(effect, vfxEffect, offset, effectTransform, currentFrame));
            }
        }

        private static Matrix4x4 CreateEffectOffset(BSA_Type6 effect)
        {
            return Matrix4x4.CreateTranslation(new SimdVector3(effect.F_12, effect.F_16, effect.F_20));
        }

        private static Matrix4x4 ApplyEffectOffset(Matrix4x4 projectileTransform, Matrix4x4 effectOffset)
        {
            return effectOffset * projectileTransform;
        }

        private void StopEffect(BSA_Type6 effect)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (!activeEffects[i].Matches(effect))
                    continue;

                activeEffects[i].Effect?.Terminate(false);
                activeEffects.RemoveAt(i);
            }

            parent?.StopEffect(effect);
        }

        private void PlayDuePassEntries(float previousFrame, float targetFrame)
        {
            foreach (BSA_Type0 passEntry in passEntries)
            {
                if (!IsPassEntryActive(passEntry, previousFrame, targetFrame))
                    continue;

                bool shouldPass;
                switch (passEntry.I_00)
                {
                    case 0:
                        shouldPass = true;
                        break;
                    case 1:
                    case 2:
                        shouldPass = hasHitEnemy;
                        break;
                    case 4:
                        SimdVector3? targetPosition = GetLinkedTargetPosition();
                        shouldPass = passEntry.I_02 == 0 && targetPosition.HasValue &&
                            SimdVector3.Distance(transform.Translation, targetPosition.Value) <= passEntry.F_08;
                        break;
                    case 5:
                        shouldPass = allowBacConditionPassEntries && HasMatchingBacPassCondition(passEntry);
                        break;
                    case 6:
                        shouldPass = !hasHitEnemy;
                        break;
                    default:
                        shouldPass = false;
                        break;
                }

                if (!shouldPass)
                    continue;

                entryPassStarted = StartPassEntry(passEntry.BSA_EntryID, BsaPassReason.SystemPass);
                if (entryPassStarted)
                    break;
            }
        }

        private bool IsEffectDue(BSA_Type6 effect, float previousFrame, float targetFrame)
        {
            return IsTimedTypeDue(effect, previousFrame, targetFrame);
        }

        private bool IsPassEntryActive(BSA_Type0 passEntry, float previousFrame, float targetFrame)
        {
            if (passEntry.Duration == 0)
                return passEntry.StartTime >= previousFrame && passEntry.StartTime <= targetFrame;

            float passStart = passEntry.StartTime;
            float passEnd = passStart + passEntry.Duration;

            if (targetFrame == 0f)
                return passStart == 0f;

            return targetFrame >= passStart && previousFrame < passEnd;
        }

        private bool HasMatchingBacPassCondition(BSA_Type0 passEntry)
        {
            return bacInstance?.HasActiveBsaPassCondition(passEntry.F_08) == true;
        }

        private bool IsTimedTypeDue(IBsaType type, float previousFrame, float targetFrame)
        {
            if (targetFrame == 0f)
                return type.StartTime == 0;

            if (previousFrame <= 0f && type.StartTime == 0)
                return true;

            return type.StartTime > previousFrame && type.StartTime <= targetFrame;
        }

        private void UpdateActiveEffectTransforms()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].Effect == null || activeEffects[i].Effect.IsDestroyed)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }

                if (activeEffects[i].SkipTransformUpdateOnCreatedFrame && activeEffects[i].CreatedFrame == currentFrame)
                {
                    activeEffects[i].SkipTransformUpdateOnCreatedFrame = false;
                    continue;
                }

                Matrix4x4 effectTransform = ApplyEffectOffset(transform, activeEffects[i].Offset);
                if (!movementChannels.IsTracking)
                {
                    Matrix4x4 fixedRotation = activeEffects[i].Transform;
                    fixedRotation.Translation = effectTransform.Translation;
                    effectTransform = fixedRotation;
                }
                activeEffects[i].Transform = effectTransform;
                activeEffects[i].Effect.SetExternalTransform(effectTransform);
            }
        }

        private void UpdateHitboxes()
        {
            foreach (BsaHitboxPreview hitboxPreview in hitboxPreviews)
                hitboxPreview.Update();
        }

        private void ApplyHitboxDamage()
        {
            if (actor == null)
                return;

            foreach (BsaHitboxPreview preview in hitboxPreviews)
            {
                if (!preview.CanHit() || !preview.TryGetBounds(out Microsoft.Xna.Framework.BoundingBox bounds))
                    continue;

                foreach (Actor target in SceneManager.Actors)
                {
                    if (target == null || target.Team == actor.Team || !target.CanBeHit(bounds, preview))
                        continue;

                    ushort bdmId = preview.GetBdmEntryId();
                    int entryIndex = shotBdm?.IndexOf(bdmId) ?? -1;
                    if (entryIndex >= 0)
                    {
                        BDM_Entry entry = shotBdm.BDM_Entries[entryIndex];
                        Matrix4x4.Invert(target.Transform, out Matrix4x4 inverseTarget);
                        SimdVector3 direction = movementChannels.Velocity.LengthSquared() > 0.000001f
                            ? SimdVector3.TransformNormal(-movementChannels.Velocity, inverseTarget)
                            : SimdVector3.Transform(transform.Translation, inverseTarget);
                        if (direction.LengthSquared() > 0f)
                            direction = SimdVector3.Normalize(direction);
                        target.Controller.ApplyDamageState(entry, direction, actor, move, transform);
                    }
                    preview.RecordHit();
                    hasHitEnemy = true;
                    foreach (BSA_Type0 passEntry in passEntries)
                    {
                        if ((passEntry.I_00 != 1 && passEntry.I_00 != 2) ||
                            !IsPassEntryActive(passEntry, currentFrame - 1f, currentFrame))
                            continue;

                        entryPassStarted = StartPassEntry(passEntry.BSA_EntryID, BsaPassReason.SystemPass);
                        if (entryPassStarted)
                        {
                            break;
                        }
                    }

                    if (!entryPassStarted && !preview.CanHit() && (bsaEntry.I_16_a & 2) != 0)
                    {
                        ushort nextEntry = bsaEntry.ImpactEnemy != ushort.MaxValue
                            ? bsaEntry.ImpactEnemy : bsaEntry.Expires;
                        StartPassEntry(nextEntry, BsaPassReason.ImpactEnemy);
                        entryPassStarted = true;
                    }

                    if (entryPassStarted)
                        return;
                }
            }
        }

        private void UpdateChildProjectiles(float frameStep, bool playAudio)
        {
            for (int i = childProjectiles.Count - 1; i >= 0; i--)
            {
                childProjectiles[i].Update(frameStep, playAudio);

                if (!childProjectiles[i].IsFinished)
                    continue;

                childProjectiles[i].Expire();
                childProjectiles.RemoveAt(i);
            }
        }

        private void TryStartPassEntry(BsaPassReason reason)
        {
            if (reason == BsaPassReason.Expires)
            {
                if (expiryPassStarted || currentFrame < expiryFrame)
                    return;

                expiryPassStarted = true;
                entryPassStarted = StartPassEntry(bsaEntry.Expires, reason);
            }
        }

        private bool StartPassEntry(ushort entryId, BsaPassReason reason)
        {
            if (entryId == ushort.MaxValue || passDepth >= MaxBsaPassDepth)
                return false;

            if (!TryGetPassEntry(entryId, out BSA_Entry entry))
                return false;

            if (entry.IBsaTypes == null)
                entry.InitializeIBsaTypes();
            childProjectiles.Add(new ProjectileInstance(bacInstance, this, actor, attachActor, move, bsaFile, entry, null, transform, passDepth + 1, allowBacConditionPassEntries, reason));
            return true;
        }

        private bool TryGetPassEntry(ushort entryId, out BSA_Entry entry)
        {
            entry = GetBsaEntries()?.FirstOrDefault(bsaEntry => bsaEntry.SortID == entryId);
            return entry != null;
        }

        private IEnumerable<BSA_Entry> GetBsaEntries()
        {
            return bsaFile?.BSA_Entries ?? move?.Files?.BsaFile?.File?.BSA_Entries;
        }

        private enum BsaPassReason
        {
            Root,
            Expires,
            ImpactProjectile,
            ImpactEnemy,
            ImpactGround,
            SystemPass
        }

        private class ActiveProjectileEffect
        {
            public BSA_Type6 Source { get; }
            public VfxEffect Effect { get; }
            public Matrix4x4 Offset { get; }
            public Matrix4x4 Transform { get; set; }
            public float CreatedFrame { get; }
            public bool SkipTransformUpdateOnCreatedFrame { get; set; }

            public ActiveProjectileEffect(BSA_Type6 source, VfxEffect effect, Matrix4x4 offset, Matrix4x4 transform, float createdFrame)
            {
                Source = source;
                Effect = effect;
                Offset = offset;
                Transform = transform;
                CreatedFrame = createdFrame;
                SkipTransformUpdateOnCreatedFrame = true;
            }

            public bool Matches(BSA_Type6 effect)
            {
                return Source.EepkType == effect.EepkType &&
                       Source.SkillID == effect.SkillID &&
                       Source.EffectID == effect.EffectID;
            }
        }

    }
}
