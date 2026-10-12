using System;
using Microsoft.Xna.Framework;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    public partial class ProjectileInstance
    {
        public static bool SimulateMovement { get; set; } = true;

        private readonly MovementChannels movementChannels = new MovementChannels();

        private void FollowOwner()
        {
            if (!followsOwner)
                return;

            Matrix4x4 ownerTransform = GetOwnerTransform();
            if (!Matrix4x4.Invert(previousOwnerTransform, out Matrix4x4 inverseOwner))
                throw new InvalidOperationException("Cannot follow a BSA owner with a singular attachment transform.");

            Matrix4x4 ownerDelta = inverseOwner * ownerTransform;
            transform *= ownerDelta;
            initialTransform *= ownerDelta;
            effectRootPosition = SimdVector3.Transform(effectRootPosition, ownerDelta);
            movementChannels.Velocity = SimdVector3.TransformNormal(movementChannels.Velocity, ownerDelta);
            previousOwnerTransform = ownerTransform;
        }

        private void RefreshWorldTransform()
        {
            if (isAttachedToSource)
            {
                transform = CreateWorldTransformFromMotion(motionTransform);
                if (movementChannels.FaceTarget && GetLinkedTargetPosition() is SimdVector3 linkedTargetPosition)
                    transform = PointAtTarget(transform, linkedTargetPosition);
            }
        }

        private void Move(float startFrame, float targetFrame)
        {
            float frame = startFrame;

            while (frame < targetFrame)
            {
                RefreshWorldTransform();
                ApplyDueMovements(movementChannels, transform, frame);

                if (isAttachedToSource && movementChannels.HasTravel)
                    DetachFromSource(frame);

                float nextFrame = GetNextMovementBoundary(movementChannels, targetFrame);
                if (!isAttachedToSource)
                    AdvanceMovement(ref transform, movementChannels, nextFrame - frame);

                frame = nextFrame;
            }
        }

        private void DetachFromSource(float frame)
        {
            detachWorldTransform = transform;
            detachFrame = frame;
            hasDetachedFromSource = true;
            transform = detachWorldTransform;
            isAttachedToSource = false;
        }

        private float GetNextMovementBoundary(MovementChannels channels, float targetFrame)
        {
            return channels.NextIndex < movements.Count
                ? Math.Min(targetFrame, movements[channels.NextIndex].StartTime)
                : targetFrame;
        }

        private void ApplyDueMovements(MovementChannels channels, Matrix4x4 targetTransform, float frame)
        {
            if (channels.NextIndex >= movements.Count || movements[channels.NextIndex].StartTime > frame)
                return;

            Actor linkedActor = GetLinkedTargetActor();
            while (channels.NextIndex < movements.Count && movements[channels.NextIndex].StartTime <= frame)
            {
                if (SimulateMovement)
                    channels.Apply(movements[channels.NextIndex], targetTransform, linkedActor?.Transform, actor);
                channels.NextIndex++;
            }
        }

        private Actor GetLinkedTargetActor()
        {
            // The preview has scene actors, but not the game's per-projectile linked-object handle.
            return actor != null ? actor.Opponent : (SceneManager.VictimEnabled ? SceneManager.Actors[1] : null);
        }

        private SimdVector3? GetLinkedTargetPosition()
        {
            return GetLinkedTargetActor()?.Transform.Translation;
        }

        private void AdvanceMovement(ref Matrix4x4 targetTransform, MovementChannels channels, float frames)
        {
            SimdVector3? linkedTargetPosition = channels.NeedsTarget ? GetLinkedTargetPosition() : null;
            while (frames > 0f)
            {
                float step = Math.Min(frames, 1f);
                float seconds = step / 60f;
                channels.TrackTarget(targetTransform.Translation, linkedTargetPosition, step);
                if (channels.FaceTarget && linkedTargetPosition.HasValue)
                    targetTransform = PointAtTarget(targetTransform, linkedTargetPosition.Value);
                channels.Velocity += channels.Acceleration * seconds;
                if ((channels.IsTracking || channels.UsesRecordDirection) && channels.Velocity.LengthSquared() > 0.000001f)
                    targetTransform = PointAtTarget(targetTransform, targetTransform.Translation + channels.Velocity);
                targetTransform.Translation += channels.Velocity * seconds;
                frames -= step;
            }
        }

        private Matrix4x4 GetProjectileTransformAtFrame(float frame)
        {
            frame = Math.Max(0f, frame);

            if (hasDetachedFromSource && frame >= detachFrame)
                return MoveTransform(detachWorldTransform, detachFrame, frame);

            if (canFollowAttachTransform && GetFirstMovementFrame(frame) < 0f)
                return CreateWorldTransformFromMotion(initialMotionTransform);

            Matrix4x4 startTransform = canFollowAttachTransform
                ? CreateWorldTransformFromMotion(initialMotionTransform)
                : initialTransform;
            return MoveTransform(startTransform, 0f, frame);
        }

        private float GetFirstMovementFrame(float maxFrame)
        {
            foreach (MovementState movement in movements)
            {
                if (movement.StartTime > maxFrame)
                    break;
                if (movement.UpdatesTravel && movement.HasMovement)
                    return movement.StartTime;
            }

            return -1f;
        }

        private Matrix4x4 MoveTransform(Matrix4x4 startTransform, float startFrame, float targetFrame)
        {
            if (targetFrame <= startFrame)
                return startTransform;

            Matrix4x4 replayTransform = initialTransform;
            MovementChannels channels = new MovementChannels();
            float frame = 0f;

            while (frame < targetFrame)
            {
                if (frame == startFrame)
                    replayTransform = startTransform;
                ApplyDueMovements(channels, replayTransform, frame);

                float nextFrame = GetNextMovementBoundary(channels, targetFrame);
                if (startFrame > frame && startFrame < nextFrame)
                    nextFrame = startFrame;

                AdvanceMovement(ref replayTransform, channels, nextFrame - frame);
                frame = nextFrame;
            }

            return replayTransform;
        }

        private class MovementState
        {
            private readonly BSA_Type1 movement;
            private const int UseRecordDirectionFlag = 0x00010000;
            private const int ObjectDirectionFlag = 0x00200000;
            private const int StateOnlyFlags = 0x101E0000;

            public SimdVector3 Velocity { get; private set; }
            public bool UpdatesTravel => (movement.I_00 & StateOnlyFlags) == 0 && (movement.I_00 & 0xFF) <= 4;
            public bool HasMovement => (movement.I_00 & 0xFF) == 4 || !MathHelpers.FloatEquals(movement.F_04, 0f);
            public bool IsStateOnly => !UpdatesTravel;
            public ushort StartTime => movement.StartTime;
            public int Operation => movement.I_00 & 0xFF;
            public BSA_Type1 Data => movement;
            private uint[] spreadRolls;
            private bool flipSpreadPitch;

            public MovementState(BSA_Type1 movement)
            {
                this.movement = movement;
            }

            public void SetVelocity(Matrix4x4 projectileTransform, Matrix4x4 ownerTransform)
            {
                if ((movement.I_00 & 0x04000000) != 0)
                {
                    SimdVector3 direction = new SimdVector3(movement.F_08, movement.F_12, movement.F_16);
                    Velocity = SimdVector3.TransformNormal(direction, GetRotationOnly(ownerTransform)) * movement.F_04;
                }
                else if ((movement.I_00 & ObjectDirectionFlag) != 0)
                {
                    SimdVector3 direction = new SimdVector3(-movement.F_08, movement.F_12, -movement.F_16);
                    Velocity = direction.X == 0f && direction.Z == 0f
                        ? direction * movement.F_04
                        : SimdVector3.TransformNormal(direction, projectileTransform) * movement.F_04;
                }
                else if ((movement.I_00 & UseRecordDirectionFlag) != 0)
                {
                    Velocity = new SimdVector3(movement.F_08, movement.F_12, movement.F_16) * movement.F_04;
                }
                else
                {
                    Velocity = SimdVector3.TransformNormal(-SimdVector3.UnitZ, projectileTransform) * movement.F_04;
                }
            }

            public void ApplySpread(Matrix4x4 projectileTransform, Matrix4x4? linkedTransform, Actor owner)
            {
                if ((movement.I_00 & 0x00C00000) == 0)
                    return;

                if (spreadRolls == null)
                {
                    if (owner == null)
                        throw new InvalidOperationException("BSA spread needs the owner actor's random streams.");

                    // Draw once per record. Transform replays apply the record again and must reuse the same rolls.
                    spreadRolls = new uint[3];
                    for (int i = 0; i < spreadRolls.Length; i++)
                        spreadRolls[i] = owner.MobRandom.GetInt();
                    flipSpreadPitch = (movement.I_00 & 0x00800000) != 0 && owner.CrtRandom.Next() % 2 == 0;
                }

                float distanceFactor = 1f;
                if ((movement.I_00 & 0x00400000) != 0)
                {
                    float range = movement.F_32 - movement.F_36;
                    distanceFactor = linkedTransform.HasValue && range > 0f
                        ? Math.Max(0f, Math.Min(1f, (SimdVector3.Distance(projectileTransform.Translation, linkedTransform.Value.Translation) - movement.F_36) / range))
                        : 0f;
                }

                float minAngle = movement.F_44 * distanceFactor;
                float maxAngle = movement.F_40 * distanceFactor;
                float angle = MathHelper.ToRadians(minAngle + (maxAngle - minAngle) * (spreadRolls[0] % 100 / 100f));
                float horizontalShare = spreadRolls[1] % 100 / 100f;
                float pitch = angle * horizontalShare;
                float yaw = angle * (1f - horizontalShare);
                if (spreadRolls[2] % 2 == 0)
                    yaw = -yaw;
                if (flipSpreadPitch)
                    pitch = -pitch;

                Matrix4x4 spreadRotation = Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, 0f);
                SimdVector3 direction = SimdVector3.TransformNormal(-SimdVector3.UnitZ, spreadRotation * projectileTransform);
                Velocity = direction * movement.F_04;
            }
        }

        private class MovementChannels
        {
            private const float MinimumTrackingLengthSquared = 0.1f;
            public int NextIndex { get; set; }
            public SimdVector3 Velocity { get; set; }
            public SimdVector3 Acceleration { get; set; }
            public bool HasTravel => Velocity.LengthSquared() > 0.000001f || Acceleration.LengthSquared() > 0.000001f;
            private bool trackTarget;
            private bool skipFirstTurn;
            private bool scaleTurnByFrames;
            private bool faceTarget;
            private float maxTrackAngle;
            private SimdVector3 trackWeights;
            public bool FaceTarget => faceTarget;
            public bool IsTracking => trackTarget;
            public bool UsesRecordDirection { get; private set; }
            public bool NeedsTarget => trackTarget || faceTarget;

            public void Apply(MovementState movement, Matrix4x4 projectileTransform, Matrix4x4? linkedTransform, Actor owner)
            {
                int flags = movement.Data.I_00;
                if ((flags & 0x10000000) != 0 || (flags & 0x00020000) != 0)
                {
                    trackTarget = true;
                    skipFirstTurn = (flags & 0x10000000) != 0;
                    scaleTurnByFrames = !skipFirstTurn;
                    maxTrackAngle = MathHelper.ToRadians(movement.Data.F_04);
                    trackWeights = new SimdVector3(movement.Data.F_20, movement.Data.F_24, movement.Data.F_28);
                    return;
                }
                if ((flags & 0x00040000) != 0)
                {
                    trackTarget = false;
                    return;
                }
                if ((flags & 0x01000000) != 0 && linkedTransform.HasValue)
                    faceTarget = true;
                if ((flags & 0x02000000) != 0)
                    faceTarget = false;

                if (movement.IsStateOnly)
                    return;

                movement.SetVelocity(projectileTransform, owner?.Transform ?? Matrix4x4.Identity);
                movement.ApplySpread(projectileTransform, linkedTransform, owner);
                switch (movement.Operation)
                {
                    case 0:
                        Velocity = movement.Velocity;
                        if (Velocity.LengthSquared() > 0.000001f)
                            UsesRecordDirection = (flags & 0x04210000) != 0;
                        break;
                    case 1:
                        Velocity += movement.Velocity;
                        if (movement.Velocity.LengthSquared() > 0.000001f && (flags & 0x04210000) != 0)
                            UsesRecordDirection = true;
                        break;
                    case 2:
                        Acceleration = movement.Velocity;
                        break;
                    case 3:
                        Acceleration += movement.Velocity;
                        break;
                    case 4:
                        Velocity = SimdVector3.Zero;
                        Acceleration = SimdVector3.Zero;
                        break;
                }
            }

            public void TrackTarget(SimdVector3 position, SimdVector3? targetPosition, float frames)
            {
                if (faceTarget && targetPosition.HasValue)
                {
                    Velocity = SimdVector3.Zero;
                    Acceleration = SimdVector3.Zero;
                    return;
                }

                if (!trackTarget || !targetPosition.HasValue || Velocity.LengthSquared() <= MinimumTrackingLengthSquared)
                    return;

                SimdVector3 toTarget = targetPosition.Value - position;
                if (toTarget.LengthSquared() <= MinimumTrackingLengthSquared)
                    return;

                SimdVector3 currentDirection = SimdVector3.Normalize(Velocity);
                SimdVector3 targetDirection = SimdVector3.Normalize(toTarget);
                float dot = Math.Max(-1f, Math.Min(1f, SimdVector3.Dot(currentDirection, targetDirection)));
                if ((float)Math.Acos(dot) > maxTrackAngle)
                {
                    trackTarget = false;
                    return;
                }

                if (skipFirstTurn)
                {
                    skipFirstTurn = false;
                    return;
                }

                SimdVector3 aimOffset = toTarget + SimdVector3.UnitY;
                if (aimOffset.LengthSquared() < 0.000001f)
                    return;

                SimdVector3 aimDirection = SimdVector3.Normalize(aimOffset);
                SimdVector3 weightedTarget = aimDirection * trackWeights;
                SimdVector3 turnedDirection = currentDirection + weightedTarget * (scaleTurnByFrames ? frames : 1f);
                if (turnedDirection.LengthSquared() > 0.000001f)
                    Velocity = SimdVector3.Normalize(turnedDirection) * Velocity.Length();
            }
        }
    }
}
