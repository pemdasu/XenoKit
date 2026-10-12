using BEPUphysics;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using Xv2CoreLib.SCD;
using BcsBone = Xv2CoreLib.BCS.Bone;
using BepuVector3 = BEPUutilities.Vector3;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Animation
{
    /// <summary>
    /// Rigid body simulation for an SCD file (hair, skirts, capes). Bodies are bound to bones of the SCD skeleton, and BCS skeleton entries add bone-driven colliders.
    /// All matrices passed in and out are world space bone globals.
    /// </summary>
    public sealed partial class ScdPhysics
    {
        public const float TimeStep = 1f / 60f;
        //ponytail: eight steps cap CPU work. If smaller bodies need shorter steps, raise the cap.
        private const int MaxSolverSteps = 8;
        public static readonly SimdVector3 Gravity = new SimdVector3(0f, -9.81f, 0f);
        public const int SolverIterations = 4;
        //Contacts deeper than this are drawn red in the debug view.
        public const float DeepPenetration = 0.02f;
        private const float MaxLinearSpeed = 200f;
        private const float MaxAngularSpeed = 100f;
        private const float MinimumShapeSize = 0.005f;
        private const float DegreesToRadians = (float)Math.PI / 180f;

        //Viewer settings. Enabled false keeps SCD bones on the animated pose. ResetOnLoop false keeps the simulation running when the timeline loops.
        public static bool Enabled = true;
        public static bool ResetOnLoop = true;
        public static float GravityScale = 1f;
        public static bool ShowColliders = false;

        private const float DefaultMaxSpeed = 10f;
        private const float DefaultConvexRadius = 0.05f;
        private const float MovingRootSpeed = 3f;
        private const float DragRate = 30f;
        private const float BcsConvexRadius = 0.01f;
        private const float BoxShrinkFraction = 0.25f;
        private const float CarryRootDistance = 0.5f;
        private const float CarryRootAngle = 45f * DegreesToRadians;

        private readonly Space space = new Space();
        private readonly List<ScdBody> bodies = new List<ScdBody>();
        private readonly ScdBody[] bodyByBone;
        private readonly List<BoneCollider> colliders = new List<BoneCollider>();
        private readonly List<SpringLink> springs = new List<SpringLink>();
        private readonly int[] parentIndices;
        private readonly Matrix4x4[] animatedWorld;
        private readonly bool[] changedBones;
        private readonly float maxSpeed = DefaultMaxSpeed;
        private readonly float rootFollow;
        private float gravityPerRadius;
        private Dictionary<Entity, int> hardShapeIndex;

        private Matrix4x4 previousRoot;
        private SimdVector3 previousRootVelocity;
        private int rootHistoryCount;

        public List<string> Warnings { get; } = new List<string>();
        public int BodyCount => bodies.Count;
        public int ColliderCount => colliders.Count;
        public int JointCount { get; private set; }

        public enum DebugShapeKind
        {
            Collider,
            BoneDrivenBody,
            DynamicBody
        }

        public int DebugShapeCount => colliders.Count + bodies.Count;

        /// <summary>
        /// Shape and current world transform of a collider (indices first) or body, for the debug view.
        /// </summary>
        public void GetDebugShape(int index, out ConvexShape shape, out Matrix4x4 world, out DebugShapeKind kind)
        {
            Entity entity;

            if (index < colliders.Count)
            {
                entity = colliders[index].Entity;
                kind = DebugShapeKind.Collider;
            }
            else
            {
                ScdBody body = bodies[index - colliders.Count];
                entity = body.Entity;
                kind = body.IsDynamic ? DebugShapeKind.DynamicBody : DebugShapeKind.BoneDrivenBody;
            }

            shape = (ConvexShape)entity.CollisionInformation.Shape;
            world = ToSimd(entity.WorldTransform);
        }

        /// <param name="boneWorld">Current world globals of the SCD skeleton, used as the rest pose for bodies and joints.</param>
        /// <param name="colliderBones">BCS skeleton entries whose bones exist in the character skeleton.</param>
        /// <param name="colliderBoneWorld">World globals of the character bones for each entry in <paramref name="colliderBones"/>.</param>
        public ScdPhysics(SCD_File scdFile, string[] boneNames, int[] parentIndices, Matrix4x4[] boneWorld, IList<BcsBone> colliderBones, Matrix4x4[] colliderBoneWorld)
        {
            if (boneNames.Length != parentIndices.Length || boneNames.Length != boneWorld.Length)
                throw new ArgumentException("ScdPhysics: bone names, parents and matrices must have the same length.");

            for (int boneIndex = 0; boneIndex < parentIndices.Length; boneIndex++)
            {
                if (parentIndices[boneIndex] >= boneIndex)
                    throw new InvalidDataException($"ScdPhysics: bone \"{boneNames[boneIndex]}\" comes before its parent. Bones must be ordered parent first.");
            }

            this.parentIndices = parentIndices;
            bodyByBone = new ScdBody[boneNames.Length];
            animatedWorld = new Matrix4x4[boneNames.Length];
            changedBones = new bool[boneNames.Length];

            space.ForceUpdater.Gravity = ToBepu(Gravity);
            space.Solver.IterationLimit = SolverIterations;
            space.TimeStepSettings.TimeStepDuration = TimeStep;

            foreach (SCD_Block block in scdFile.Blocks)
            {
                if (block.Config0 != null)
                {
                    maxSpeed = block.Config0.MaxSpeed;
                }
                else if (block.Config1 != null)
                {
                    maxSpeed = block.Config1.MaxSpeed;
                    rootFollow = block.Config1.RootFollow;
                }
                else if (block.Body != null)
                {
                    AddBody(block.Tag, block.Body, boneNames, boneWorld);
                }
                else if (block.Tag.StartsWith("BDY"))
                {
                    Warnings.Add($"SCD: {block.Tag} is not used by {(scdFile.IsVersion1 ? "version 1" : "version 2")} files and was skipped.");
                }
            }

            AddDynamicCollisionRules();

            //Joints need every body, so they are made in a second pass.
            foreach (SCD_Block block in scdFile.Blocks)
            {
                if (block.Constraint != null)
                    AddJoint(block.Tag, block.Constraint);
                else if (block.Spring != null)
                    AddSpring(block.Spring);
            }

            for (int index = 0; index < colliderBones.Count; index++)
                AddCollider(colliderBones[index], index, colliderBoneWorld[index]);
        }

        #region Simulation
        /// <summary>
        /// Snap every body to its bone and clear velocities and root motion history. Use after a seek or a teleport.
        /// </summary>
        public void Reset(Matrix4x4[] boneWorld, Matrix4x4[] colliderBoneWorld)
        {
            foreach (ScdBody body in bodies)
            {
                body.Entity.WorldTransform = ToBepu(body.GetEntityWorld(boneWorld[body.BoneIndex]));
                body.Entity.LinearVelocity = BepuVector3.Zero;
                body.Entity.AngularVelocity = BepuVector3.Zero;
            }

            foreach (BoneCollider collider in colliders)
            {
                collider.Entity.WorldTransform = ToBepu(collider.GetEntityWorld(colliderBoneWorld[collider.BoneSlot]));
                collider.Entity.LinearVelocity = BepuVector3.Zero;
                collider.Entity.AngularVelocity = BepuVector3.Zero;
            }

            rootHistoryCount = 0;
        }

        /// <summary>
        /// Treat the current root as the previous one and drop the root motion history. Use when the root moved without a step.
        /// </summary>
        public void SkipRootMove(Matrix4x4 root)
        {
            previousRoot = root;
            rootHistoryCount = 0;
        }

        /// <summary>
        /// Advance the simulation by one <see cref="TimeStep"/>. <paramref name="boneWorld"/> holds the animated pose on input and the simulated pose on output.
        /// </summary>
        /// <param name="root">World matrix of the physics part's attachment bone.</param>
        public void Step(Matrix4x4[] boneWorld, Matrix4x4[] colliderBoneWorld, Matrix4x4 root)
        {
            Array.Copy(boneWorld, animatedWorld, boneWorld.Length);
            CarryLargeRootJump(root);

            foreach (ScdBody body in bodies)
            {
                if (body.IsDynamic)
                {
                    body.Entity.Gravity = ToBepu(Gravity * (body.GravityFactor * GravityScale));
                    continue;
                }

                body.AnimatedEntityWorld = body.GetEntityWorld(animatedWorld[body.BoneIndex]);
                MoveKinematic(body.Entity, body.AnimatedEntityWorld);
            }

            foreach (BoneCollider collider in colliders)
            {
                collider.AnimatedEntityWorld = collider.GetEntityWorld(colliderBoneWorld[collider.BoneSlot]);
                MoveKinematic(collider.Entity, collider.AnimatedEntityWorld);
            }

            ApplyRootMotion(root);

            //Keep gravity travel within each body's outer radius during each solver step.
            int solverSteps = Math.Max(1, Math.Min(MaxSolverSteps,
                (int)Math.Ceiling(TimeStep * Math.Sqrt(Gravity.Length() * Math.Abs(GravityScale) * gravityPerRadius))));
            float solverTimeStep = TimeStep / solverSteps;
            space.TimeStepSettings.TimeStepDuration = solverTimeStep;

            for (int step = 0; step < solverSteps; step++)
            {
                foreach (SpringLink spring in springs)
                    spring.Apply(solverTimeStep);

                space.Update();
            }
            ClampDynamicSpeeds();

            //Kinematic bodies end the step exactly on their bone.
            foreach (ScdBody body in bodies)
            {
                if (!body.IsDynamic)
                    body.Entity.WorldTransform = ToBepu(body.AnimatedEntityWorld);
            }

            foreach (BoneCollider collider in colliders)
                collider.Entity.WorldTransform = ToBepu(collider.AnimatedEntityWorld);

            WriteBones(boneWorld);
        }

        private void ClampDynamicSpeeds()
        {
            foreach (ScdBody body in bodies)
            {
                if (!body.IsDynamic) continue;

                BepuVector3 linearVelocity = body.Entity.LinearVelocity;
                BepuVector3 angularVelocity = body.Entity.AngularVelocity;

                if (linearVelocity.LengthSquared() > MaxLinearSpeed * MaxLinearSpeed)
                    body.Entity.LinearVelocity = linearVelocity * (MaxLinearSpeed / linearVelocity.Length());

                if (angularVelocity.LengthSquared() > MaxAngularSpeed * MaxAngularSpeed)
                    body.Entity.AngularVelocity = angularVelocity * (MaxAngularSpeed / angularVelocity.Length());
            }
        }

        private static void MoveKinematic(Entity entity, Matrix4x4 targetWorld)
        {
            //Velocities let the solver see the motion. The pose is set again after the step.
            BepuVector3 targetPosition = ToBepu(targetWorld.Translation);
            BEPUutilities.Quaternion targetOrientation = BEPUutilities.Quaternion.CreateFromRotationMatrix(ToBepu(targetWorld));
            BEPUutilities.Quaternion delta = BEPUutilities.Quaternion.Concatenate(BEPUutilities.Quaternion.Inverse(entity.Orientation), targetOrientation);

            if (delta.W < 0f)
                delta = BEPUutilities.Quaternion.Negate(delta);

            BEPUutilities.Quaternion.GetAxisAngleFromQuaternion(ref delta, out BepuVector3 axis, out float angle);

            entity.LinearVelocity = (targetPosition - entity.Position) / TimeStep;
            entity.AngularVelocity = float.IsNaN(angle) ? BepuVector3.Zero : axis * (angle / TimeStep);
        }

        /// <summary>
        /// The viewer can move or turn the root much farther in one update than the game does (BAC restarts, timeline loops, instant turns).
        /// Driving the bone-driven bodies across that in one step flings the dynamic bodies away, so every body is carried with the root instead.
        /// </summary>
        private void CarryLargeRootJump(Matrix4x4 root)
        {
            if (rootHistoryCount == 0)
                return;

            bool jumped = (root.Translation - previousRoot.Translation).LengthSquared() > CarryRootDistance * CarryRootDistance;

            if (!jumped)
            {
                SimdQuaternion previousTurn = SimdQuaternion.CreateFromRotationMatrix(RemoveScale(previousRoot, out _));
                SimdQuaternion currentTurn = SimdQuaternion.CreateFromRotationMatrix(RemoveScale(root, out _));
                float halfAngleCos = (float)Math.Cos(CarryRootAngle * 0.5f);
                jumped = Math.Abs(SimdQuaternion.Dot(previousTurn, currentTurn)) < halfAngleCos;
            }

            if (!jumped || !Matrix4x4.Invert(previousRoot, out Matrix4x4 inversePreviousRoot))
                return;

            Matrix4x4 rootChange = inversePreviousRoot * root;

            foreach (ScdBody body in bodies)
                CarryEntity(body.Entity, rootChange);

            foreach (BoneCollider collider in colliders)
                CarryEntity(collider.Entity, rootChange);

            rootHistoryCount = 0;
        }

        private static void CarryEntity(Entity entity, Matrix4x4 rootChange)
        {
            entity.WorldTransform = ToBepu(RemoveScale(ToSimd(entity.WorldTransform) * rootChange, out _));
            entity.LinearVelocity = ToBepu(SimdVector3.TransformNormal(ToSimd(entity.LinearVelocity), rootChange));
            entity.AngularVelocity = ToBepu(SimdVector3.TransformNormal(ToSimd(entity.AngularVelocity), rootChange));
        }

        private void ApplyRootMotion(Matrix4x4 root)
        {
            SimdVector3 rootVelocity = rootHistoryCount > 0 ? (root.Translation - previousRoot.Translation) / TimeStep : SimdVector3.Zero;
            SimdVector3 rootVelocityChange = rootHistoryCount > 1 ? rootVelocity - previousRootVelocity : SimdVector3.Zero;
            float rootSpeed = rootVelocity.Length();
            bool rootIsMoving = rootSpeed >= MovingRootSpeed;
            //Moving drag pushes against the root velocity, limited to the max speed. A max speed of 0 or less turns it off.
            SimdVector3 dragVelocity = rootSpeed > maxSpeed ? rootVelocity * (maxSpeed / rootSpeed) : rootVelocity;

            foreach (ScdBody body in bodies)
            {
                if (!body.IsDynamic) continue;

                SimdVector3 velocity = ToSimd(body.Entity.LinearVelocity) + rootVelocityChange * rootFollow;

                if (rootIsMoving)
                {
                    if (maxSpeed > 0f)
                        velocity -= dragVelocity * (body.Drag * DragRate * TimeStep);
                }
                else if (velocity.Length() >= MovingRootSpeed)
                {
                    velocity -= velocity * (body.IdleDamping * DragRate * TimeStep);
                }

                body.Entity.LinearVelocity = ToBepu(velocity);
            }

            previousRoot = root;
            previousRootVelocity = rootVelocity;
            rootHistoryCount = Math.Min(rootHistoryCount + 1, 2);
        }

        private void WriteBones(Matrix4x4[] boneWorld)
        {
            Array.Clear(changedBones, 0, changedBones.Length);

            for (int boneIndex = 0; boneIndex < boneWorld.Length; boneIndex++)
            {
                int parentIndex = parentIndices[boneIndex];
                ScdBody body = bodyByBone[boneIndex];

                if (body != null && body.IsDynamic)
                {
                    Matrix4x4 simulated = body.BoneFromEntity * ToSimd(body.Entity.WorldTransform);

                    if (parentIndex >= 0)
                        simulated = ApplyBoneLimits(body, simulated, animatedWorld[boneIndex], animatedWorld[parentIndex], boneWorld[parentIndex]);

                    boneWorld[boneIndex] = simulated;
                    changedBones[boneIndex] = true;
                }
                else if (parentIndex >= 0 && changedBones[parentIndex])
                {
                    if (!Matrix4x4.Invert(animatedWorld[parentIndex], out Matrix4x4 inverseAnimatedParent))
                        continue;

                    boneWorld[boneIndex] = animatedWorld[boneIndex] * inverseAnimatedParent * boneWorld[parentIndex];
                    changedBones[boneIndex] = true;
                }
            }
        }

        private static Matrix4x4 ApplyBoneLimits(ScdBody body, Matrix4x4 simulatedWorld, Matrix4x4 animatedWorld, Matrix4x4 animatedParentWorld, Matrix4x4 parentWorld)
        {
            bool lockPivot = (body.Flags & 4) != 0;
            bool limitHinge = (body.Flags & 3) == 1;

            if (!lockPivot && !limitHinge)
                return simulatedWorld;

            if (!Matrix4x4.Invert(parentWorld, out Matrix4x4 inverseParent) ||
                !Matrix4x4.Invert(animatedParentWorld, out Matrix4x4 inverseAnimatedParent))
                return animatedWorld;

            Matrix4x4 local = simulatedWorld * inverseParent;
            Matrix4x4 animatedLocal = animatedWorld * inverseAnimatedParent;

            if (!Matrix4x4.Decompose(local, out SimdVector3 scale, out SimdQuaternion rotation, out SimdVector3 translation))
                return simulatedWorld;

            if (limitHinge && body.LimitAxis != SimdVector3.Zero)
            {
                if (!Matrix4x4.Decompose(animatedLocal, out _, out SimdQuaternion animatedRotation, out _))
                    return simulatedWorld;

                rotation = animatedRotation * ClampTwist(SimdQuaternion.Inverse(animatedRotation) * rotation, body.LimitAxis, body.LimitMin, body.LimitMax);
            }

            Matrix4x4 scaledRotation = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation);

            //The pivot point of the bone stays where the animated pose puts it in the parent, and the bone turns around it.
            if (lockPivot)
                translation = SimdVector3.Transform(body.Pivot, animatedLocal) - SimdVector3.TransformNormal(body.Pivot, scaledRotation);

            local = scaledRotation * Matrix4x4.CreateTranslation(translation);
            return local * parentWorld;
        }

        private static SimdQuaternion ClampTwist(SimdQuaternion rotation, SimdVector3 axis, float minAngle, float maxAngle)
        {
            if (rotation.W < 0f)
                rotation = SimdQuaternion.Negate(rotation);

            float axisPart = SimdVector3.Dot(new SimdVector3(rotation.X, rotation.Y, rotation.Z), axis);
            float twistAngle = 2f * (float)Math.Atan2(axisPart, rotation.W);
            float clampedAngle = Math.Max(minAngle, Math.Min(maxAngle, twistAngle));

            if (clampedAngle == twistAngle)
                return rotation;

            SimdQuaternion twist = SimdQuaternion.CreateFromAxisAngle(axis, twistAngle);
            SimdQuaternion swing = rotation * SimdQuaternion.Inverse(twist);
            return swing * SimdQuaternion.CreateFromAxisAngle(axis, clampedAngle);
        }

        /// <summary>
        /// Largest linear speed of the dynamic bodies.
        /// </summary>
        public float GetMaxDynamicSpeed()
        {
            float maxDynamicSpeed = 0f;

            foreach (ScdBody body in bodies)
            {
                if (body.IsDynamic)
                    maxDynamicSpeed = Math.Max(maxDynamicSpeed, body.Entity.LinearVelocity.Length());
            }

            return maxDynamicSpeed;
        }

        /// <summary>
        /// Largest distance between a dynamic body's bone in <paramref name="boneWorld"/> and where the body itself puts that bone. Large values mean bodies left their bones behind.
        /// </summary>
        public float GetMaxBodyOffset(Matrix4x4[] boneWorld)
        {
            float maxOffset = 0f;

            foreach (ScdBody body in bodies)
            {
                if (body.IsDynamic)
                    maxOffset = Math.Max(maxOffset, SimdVector3.Distance(boneWorld[body.BoneIndex].Translation, (body.BoneFromEntity * ToSimd(body.Entity.WorldTransform)).Translation));
            }

            return maxOffset;
        }

        /// <summary>
        /// Contact count and deepest penetration between dynamic bodies and BCS colliders or bone-driven bodies, from the last step.
        /// </summary>
        public void GetHardContactStats(out int contactCount, out float deepestPenetration) => GetHardContactStats(out contactCount, out deepestPenetration, out _);

        public void GetHardContactStats(out int contactCount, out float deepestPenetration, out string deepestBone)
        {
            int count = 0;
            float deepest = 0f;
            string bone = "";

            ForEachHardContact((bodyShape, otherShape, depth) =>
            {
                count++;

                if (depth > deepest)
                {
                    deepest = depth;
                    bone = GetShapeName(bodyShape);
                }
            });

            contactCount = count;
            deepestPenetration = deepest;
            deepestBone = bone;
        }

        /// <summary>
        /// Marks every debug shape that has a contact deeper than <see cref="DeepPenetration"/> in <paramref name="deepByShape"/> (indexed like <see cref="GetDebugShape"/>), and adds each such body and collider pair with its deepest depth to <paramref name="pairDepths"/> when it is not null.
        /// </summary>
        public void FindDeepContacts(bool[] deepByShape, Dictionary<string, float> pairDepths)
        {
            Array.Clear(deepByShape, 0, deepByShape.Length);

            ForEachHardContact((bodyShape, otherShape, depth) =>
            {
                if (depth <= DeepPenetration) return;

                deepByShape[bodyShape] = true;
                deepByShape[otherShape] = true;

                if (pairDepths == null) return;

                string pair = $"{GetShapeName(bodyShape)} / {GetShapeName(otherShape)}";
                pairDepths.TryGetValue(pair, out float knownDepth);
                pairDepths[pair] = Math.Max(knownDepth, depth);
            });
        }

        private string GetShapeName(int shapeIndex) => shapeIndex < colliders.Count ? colliders[shapeIndex].BoneName : bodies[shapeIndex - colliders.Count].BoneName;

        /// <summary>
        /// Calls <paramref name="onContact"/> with the debug shape index of a dynamic body, the debug shape index of the collider or bone-driven body it touches, and the penetration depth.
        /// </summary>
        private void ForEachHardContact(Action<int, int, float> onContact)
        {
            if (hardShapeIndex == null)
            {
                hardShapeIndex = new Dictionary<Entity, int>();

                for (int index = 0; index < colliders.Count; index++)
                    hardShapeIndex[colliders[index].Entity] = index;

                for (int index = 0; index < bodies.Count; index++)
                {
                    if (!bodies[index].IsDynamic)
                        hardShapeIndex[bodies[index].Entity] = colliders.Count + index;
                }
            }

            for (int index = 0; index < bodies.Count; index++)
            {
                ScdBody body = bodies[index];

                if (!body.IsDynamic) continue;

                foreach (BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair in body.Entity.CollisionInformation.Pairs)
                {
                    Entity other = pair.EntityA == body.Entity ? pair.EntityB : pair.EntityA;

                    if (other == null || !hardShapeIndex.TryGetValue(other, out int otherShape)) continue;

                    foreach (BEPUphysics.NarrowPhaseSystems.Pairs.ContactInformation contact in pair.Contacts)
                        onContact(colliders.Count + index, otherShape, contact.Contact.PenetrationDepth);
                }
            }
        }

        public int DynamicBodyCount
        {
            get
            {
                int count = 0;

                foreach (ScdBody body in bodies)
                {
                    if (body.IsDynamic) count++;
                }

                return count;
            }
        }
        #endregion
    }
}
