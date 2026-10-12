using System;
using System.Linq;
using System.Collections.Generic;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.BDM;
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
        private readonly bool allowBacConditionPassEntries;
        private Matrix4x4 initialTransform;
        private SimdVector3 effectRootPosition;
        private readonly SimdVector3 spawnOffset;
        private readonly bool followsOwner;
        private Matrix4x4 previousOwnerTransform;
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
        private readonly bool loops;
        private readonly int passDepth;
        private const int MaxBsaPassDepth = 16;
        private bool expiryPassStarted;
        private bool hasHitEnemy;
        private bool entryPassStarted;

        private float currentFrame;
        private float elapsedFrame;
        private Matrix4x4 transform;
        private Matrix4x4 motionTransform;
        private bool isAttachedToSource;
        private bool hasDetachedFromSource;
        private float detachFrame;
        private Matrix4x4 detachWorldTransform;

        public bool IsFinished => (entryPassStarted || (!loops && currentFrame >= expiryFrame)) && childProjectiles.Count == 0;
        public override Matrix4x4 Transform { get => transform; set => transform = value; }
        public float CurrentFrame => elapsedFrame;
        public int EndFrame => expiryFrame;

        public ProjectileInstance(BacEntryInstance bacInstance, BAC_Type9 projectileType, BSA_Entry bsaEntry, BSA_File bsaFile)
            : this(bacInstance, null, bacInstance?.User, GetSpawnActor(bacInstance?.User, projectileType), bacInstance?.SkillMove, bsaFile, bsaEntry, projectileType, CreateSpawnTransform(bacInstance, projectileType), 0, true)
        {
            if (projectileType != null && projectileType.SpawnSource >= 4)
                Log.Add($"BAC Type 9 spawn source {projectileType.SpawnSource} is not simulated. The projectile preview uses the user's bone transform.", LogType.Warning);
        }

        public static ProjectileInstance CreatePreview(Actor actor, Move move, BSA_Entry bsaEntry, BSA_File bsaFile, Matrix4x4 spawnTransform)
        {
            actor?.ResetRandom();
            return new ProjectileInstance(null, null, actor, actor, move, bsaFile, bsaEntry, null, spawnTransform, 0, false);
        }

        private ProjectileInstance(BacEntryInstance bacInstance, ProjectileInstance parent, Actor actor, Actor attachActor, Move move, BSA_File bsaFile, BSA_Entry bsaEntry, BAC_Type9 projectileType, Matrix4x4 spawnTransform, int passDepth, bool allowBacConditionPassEntries)
        {
            this.bacInstance = bacInstance;
            this.projectileType = projectileType ?? parent?.projectileType;
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
            this.allowBacConditionPassEntries = allowBacConditionPassEntries;
            spawnOffset = GetSpawnOffset(bsaEntry);
            followsOwner = (bsaEntry.I_17 & 1) != 0;
            initialUserDirectionAttachRotation = GetUserDirectionAttachRotation(attachActor, this.projectileType);
            previousOwnerTransform = GetOwnerTransform();
            canFollowAttachTransform = parent == null && ShouldFollowLiveAttachTransform(projectileType);
            isAttachedToSource = canFollowAttachTransform && !followsOwner;
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
            effectRootPosition = parent?.effectRootPosition ?? spawnTransform.Translation;
            if (!canFollowAttachTransform)
                transform.Translation += spawnOffset;
            initialTransform = transform;
            expiryFrame = Math.Max((int)bsaEntry.Lifetime, 1);
            loops = (bsaEntry.I_16_a & 1) != 0;
            if (loops && bsaEntry.LoopStartFrame >= expiryFrame)
                throw new System.IO.InvalidDataException($"BSA entry {bsaEntry.SortID} Loop Start Frame must be less than Lifetime.");
        }

        public void Update(float frameStep, bool playAudio = true)
        {
            elapsedFrame += frameStep;
            if (entryPassStarted)
            {
                UpdateChildProjectiles(frameStep, playAudio);
                return;
            }

            FollowOwner();
            float entryFrameStep = (bsaEntry.I_17 & 2) != 0 ? frameStep * (actor?.AnimationTimeScale ?? 1f) : frameStep;
            if (entryFrameStep <= 0f)
                UpdateFrame(0f, playAudio);
            else if (currentFrame >= expiryFrame)
                TryExpire();

            while (entryFrameStep > 0f && !entryPassStarted && currentFrame < expiryFrame)
            {
                float step = Math.Min(entryFrameStep, Math.Min(1f, expiryFrame - currentFrame));
                UpdateFrame(step, playAudio);
                entryFrameStep -= step;
            }

            UpdateChildProjectiles(frameStep, playAudio);
        }

        private void UpdateFrame(float frameStep, bool playAudio)
        {
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
                TryExpire();

            UpdateActiveEffectTransforms();
            UpdateHitboxes();
            if (frameStep > 0f && !entryPassStarted)
                ApplyHitboxDamage();
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

        private void PlayDuePassEntries(float previousFrame, float targetFrame)
        {
            foreach (BSA_Type0 passEntry in passEntries)
            {
                if (!IsPassEntryActive(passEntry, previousFrame, targetFrame))
                    continue;

                bool shouldPass;
                switch (passEntry.Condition)
                {
                    case 0:
                        shouldPass = true;
                        break;
                    case 1:
                    case 2:
                    case 10:
                        shouldPass = hasHitEnemy;
                        break;
                    case 4:
                        shouldPass = IsWithin(GetLinkedTargetPosition(), passEntry.ConditionValue);
                        break;
                    case 5:
                        shouldPass = allowBacConditionPassEntries && HasMatchingBacPassCondition(passEntry);
                        break;
                    case 6:
                        shouldPass = !hasHitEnemy;
                        break;
                    case 9:
                        shouldPass = IsWithin(actor?.Transform.Translation, passEntry.ConditionValue);
                        break;
                    default:
                        shouldPass = false;
                        break;
                }

                if (shouldPass && RunPassEntry(passEntry))
                    break;
            }
        }

        private static bool IsHitPassCondition(BSA_Type0 passEntry)
        {
            return passEntry.Condition == 1 || passEntry.Condition == 2 || passEntry.Condition == 10;
        }

        private bool IsWithin(SimdVector3? point, float radius)
        {
            return point.HasValue && SimdVector3.Distance(transform.Translation, point.Value) <= radius;
        }

        private bool RunPassEntry(BSA_Type0 passEntry)
        {
            bool switchEntry = (passEntry.ConditionFlags & 0x1) == 0;
            bool jumpTime = (passEntry.ConditionFlags & 0x2) == 0;
            float jumpFrame = Math.Max(0, (int)passEntry.JumpFrame);

            if (switchEntry)
            {
                entryPassStarted = StartPassEntry(passEntry.BSA_EntryID, jumpTime ? jumpFrame : 0f);
                if (entryPassStarted)
                    return true;
            }

            if (!jumpTime)
                return false;

            JumpToFrame(jumpFrame);
            return true;
        }

        private void JumpToFrame(float frame)
        {
            currentFrame = frame;
            playedEffects.RemoveWhere(x => x.StartTime >= frame);
            playedSounds.RemoveWhere(x => x.StartTime >= frame);
            playedScreenEffects.RemoveWhere(x => x.StartTime >= frame);
            int nextMovement = movements.FindIndex(x => x.StartTime >= frame);
            movementChannels.NextIndex = nextMovement >= 0 ? nextMovement : movements.Count;
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
            return bacInstance?.HasActiveBsaPassCondition(passEntry.ConditionValue) == true;
        }

        private bool IsTimedTypeDue(IBsaType type, float previousFrame, float targetFrame)
        {
            if (targetFrame == 0f)
                return type.StartTime == 0;

            if (previousFrame <= 0f && type.StartTime == 0)
                return true;

            return type.StartTime > previousFrame && type.StartTime <= targetFrame;
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
                        if (IsHitPassCondition(passEntry) &&
                            IsPassEntryActive(passEntry, currentFrame - 1f, currentFrame) &&
                            RunPassEntry(passEntry))
                            return;
                    }

                    if (!entryPassStarted && !preview.CanHit() && (bsaEntry.I_16_a & 2) != 0)
                    {
                        ushort nextEntry = bsaEntry.ImpactEnemy != ushort.MaxValue
                            ? bsaEntry.ImpactEnemy : bsaEntry.Expires;
                        StartPassEntry(nextEntry);
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

        private void TryExpire()
        {
            if (expiryPassStarted || currentFrame < expiryFrame)
                return;

            if (loops)
            {
                currentFrame = bsaEntry.LoopStartFrame + (currentFrame - expiryFrame) % (expiryFrame - bsaEntry.LoopStartFrame);
                return;
            }

            expiryPassStarted = true;
            entryPassStarted = StartPassEntry(bsaEntry.Expires);
        }

        private bool StartPassEntry(ushort entryId, float startFrame = 0f)
        {
            if (entryId == ushort.MaxValue || passDepth >= MaxBsaPassDepth)
                return false;

            if (!TryGetPassEntry(entryId, out BSA_Entry entry))
                return false;

            if (entry.IBsaTypes == null)
                entry.InitializeIBsaTypes();
            var child = new ProjectileInstance(bacInstance, this, actor, attachActor, move, bsaFile, entry, null, transform, passDepth + 1, allowBacConditionPassEntries);
            child.JumpToFrame(startFrame);
            childProjectiles.Add(child);
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


    }
}
