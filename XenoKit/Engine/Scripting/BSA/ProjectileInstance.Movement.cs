using System;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    public partial class ProjectileInstance
    {
        private readonly MovementChannels movementChannels = new MovementChannels();

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
                channels.Apply(movements[channels.NextIndex], targetTransform, linkedActor?.Transform);
                channels.NextIndex++;
            }
        }

        private Actor GetLinkedTargetActor()
        {
            // The preview has scene actors, but not the game's per-projectile linked-object handle.
            return SceneManager.Actors[1] == actor ? SceneManager.Actors[0] : SceneManager.Actors[1];
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

            public MovementState(BSA_Type1 movement)
            {
                this.movement = movement;
            }

            public void SetVelocity(Matrix4x4 projectileTransform)
            {
                if ((movement.I_00 & ObjectDirectionFlag) != 0)
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

            public void ApplySpread(Matrix4x4 projectileTransform, Matrix4x4 linkedTransform)
            {
                if ((movement.I_00 & 0x00C00000) == 0)
                    return;

                float distanceFactor = 1f;
                if ((movement.I_00 & 0x00400000) != 0)
                {
                    float distance = SimdVector3.Distance(projectileTransform.Translation, linkedTransform.Translation);
                    float range = movement.F_32 - movement.F_36;
                    distanceFactor = range > 0f
                        ? Math.Max(0f, Math.Min(1f, (distance - movement.F_36) / range))
                        : 0f;
                }

                // The game samples spread from its RNG. A stable sample keeps timeline scrubbing repeatable.
                int seed = movement.I_00 ^ movement.StartTime ^ projectileTransform.Translation.GetHashCode();
                Random random = new Random(seed);
                float minAngle = movement.F_44 * distanceFactor;
                float maxAngle = movement.F_40 * distanceFactor;
                float angle = (minAngle + (maxAngle - minAngle) * (float)random.NextDouble()) * ((float)Math.PI / 180f);
                if (random.Next(2) == 0)
                    angle = -angle;

                float horizontalShare = (float)random.NextDouble();
                float yaw = angle * horizontalShare;
                float pitch = angle * (1f - horizontalShare);
                if ((movement.I_00 & unchecked((int)0x80000000)) != 0 && random.Next(2) == 0)
                    pitch = -pitch;

                Matrix4x4 spreadRotation = Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, 0f);
                SimdVector3 direction = SimdVector3.TransformNormal(-SimdVector3.UnitZ, spreadRotation * projectileTransform);
                Velocity = direction * movement.F_04;
            }
        }

        private class MovementChannels
        {
            public int NextIndex { get; set; }
            public SimdVector3 Velocity { get; set; }
            public SimdVector3 Acceleration { get; set; }
            public bool HasTravel => Velocity.LengthSquared() > 0.000001f || Acceleration.LengthSquared() > 0.000001f;
            private bool trackTarget;
            private bool skipFirstTurn;
            private bool faceTarget;
            private float maxTrackAngle;
            private SimdVector3 trackWeights;
            public bool FaceTarget => faceTarget;
            public bool IsTracking => trackTarget;
            public bool UsesRecordDirection { get; private set; }
            public bool NeedsTarget => trackTarget || faceTarget;

            public void Apply(MovementState movement, Matrix4x4 projectileTransform, Matrix4x4? linkedTransform)
            {
                int flags = movement.Data.I_00;
                if ((flags & 0x10000000) != 0 || (flags & 0x00020000) != 0)
                {
                    trackTarget = true;
                    skipFirstTurn = (flags & 0x10000000) != 0;
                    maxTrackAngle = movement.Data.F_04 * ((float)Math.PI / 180f);
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

                movement.SetVelocity(projectileTransform);
                if (linkedTransform.HasValue)
                    movement.ApplySpread(projectileTransform, linkedTransform.Value);
                switch (movement.Operation)
                {
                    case 0:
                        Velocity = movement.Velocity;
                        if (Velocity.LengthSquared() > 0.000001f)
                            UsesRecordDirection = (flags & 0x00210000) != 0;
                        break;
                    case 1:
                        Velocity += movement.Velocity;
                        if (movement.Velocity.LengthSquared() > 0.000001f && (flags & 0x00210000) != 0)
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

                if (!trackTarget || !targetPosition.HasValue || Velocity.LengthSquared() < 0.000001f)
                    return;

                SimdVector3 toTarget = targetPosition.Value - position;
                if (toTarget.LengthSquared() < 0.000001f)
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
                SimdVector3 turnedDirection = currentDirection + weightedTarget * frames;
                if (turnedDirection.LengthSquared() > 0.000001f)
                    Velocity = SimdVector3.Normalize(turnedDirection) * Velocity.Length();
            }
        }
    }
}
