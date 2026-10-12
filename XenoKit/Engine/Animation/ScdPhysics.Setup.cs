using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.Constraints.SolverGroups;
using BEPUphysics.Constraints.TwoEntity.JointLimits;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Entities;
using System;
using System.IO;
using Xv2CoreLib.SCD;
using BcsBone = Xv2CoreLib.BCS.Bone;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Animation
{
    public sealed partial class ScdPhysics
    {
        #region Setup
        private void AddBody(string tag, SCD_Body scdBody, string[] boneNames, Matrix4x4[] boneWorld)
        {
            string boneName = scdBody.Bone;
            int boneIndex = Array.IndexOf(boneNames, boneName);

            if (boneIndex == -1)
                return;

            float convexRadius = DefaultConvexRadius;
            SimdVector3 pivot = SimdVector3.Zero;
            float linearDamping;
            float angularDamping;
            float dynamicFriction;
            float staticFriction;
            float restitution;
            float idleDamping = 0f;
            float gravityFactor = 1f;

            //Version 1 property names predate the material and damping field mapping.
            switch (scdBody)
            {
                case SCD_Body3 body3:
                    linearDamping = body3.Friction;
                    angularDamping = body3.F_120;
                    dynamicFriction = staticFriction = body3.Damping;
                    restitution = body3.F_108;
                    gravityFactor = body3.F_124;
                    break;
                case SCD_Body4 body4:
                    convexRadius = body4.ConvexRadius;
                    linearDamping = body4.Friction;
                    angularDamping = body4.F_124;
                    dynamicFriction = staticFriction = body4.Damping;
                    restitution = body4.F_112;
                    gravityFactor = body4.F_128;
                    idleDamping = body4.IdleDamping;
                    break;
                case SCD_Body5 body5:
                    convexRadius = body5.ConvexRadius;
                    pivot = ToSimd(body5.Pivot);
                    linearDamping = body5.Friction;
                    angularDamping = body5.F_136;
                    dynamicFriction = staticFriction = body5.Damping;
                    restitution = body5.F_124;
                    gravityFactor = body5.F_140;
                    idleDamping = body5.IdleDamping;
                    break;
                case SCD_BodyV2 bodyV2:
                    convexRadius = bodyV2.ConvexRadius;
                    pivot = ToSimd(bodyV2.Pivot);
                    linearDamping = bodyV2.LinearDamping;
                    angularDamping = bodyV2.AngularDamping;
                    dynamicFriction = bodyV2.F_124;
                    staticFriction = bodyV2.F_128;
                    restitution = bodyV2.F_132;
                    idleDamping = bodyV2.IdleDamping;
                    gravityFactor = bodyV2.GravityFactor;
                    break;
                default:
                    throw new InvalidDataException($"ScdPhysics: {tag} has an unknown body layout.");
            }

            SimdVector3 rotation = ToSimd(scdBody.ShapeRotation) * DegreesToRadians;
            Matrix4x4 shapeLocal = Matrix4x4.CreateRotationX(rotation.X) * Matrix4x4.CreateRotationY(rotation.Y) * Matrix4x4.CreateRotationZ(rotation.Z);
            shapeLocal.Translation = ToSimd(scdBody.ShapeTranslation);

            Matrix4x4 frameWorld = RemoveScale(shapeLocal * boneWorld[boneIndex], out SimdVector3 scale);
            float[] dims = scdBody.GetShapeDims();
            ConvexShape shape = CreateScdShape(scdBody.ShapeType, dims, convexRadius, scale, out float centerY);

            if (shape == null)
            {
                Warnings.Add($"SCD: {tag} for \"{boneName}\" has unsupported shape type {(int)scdBody.ShapeType} and was skipped.");
                return;
            }

            //Version 1 files use 4 (keyframed) and 5 (fixed). Version 2 files use 0 (fixed), 1 (keyframed) and 2 (dynamic). Fixed bodies follow their bone like keyframed ones.
            bool isDynamic = IsMotionDynamic(scdBody.MotionType, !(scdBody is SCD_BodyV2));

            ScdBody body = new ScdBody()
            {
                BoneName = boneName,
                BoneIndex = boneIndex,
                ShapeLocal = shapeLocal,
                CenterOffset = new SimdVector3(0f, centerY, 0f),
                Scale = scale,
                FrameWorld = frameWorld,
                Flags = (int)scdBody.Flags,
                Pivot = pivot,
                LimitAxis = SafeNormalize(ToSimd(scdBody.LimitAxis)),
                IsDynamic = isDynamic,
                GravityFactor = gravityFactor
            };

            float firstAngle = ClampAngle(scdBody.LimitAngle1);
            float secondAngle = ClampAngle(scdBody.LimitAngle2);
            body.LimitMin = Math.Min(firstAngle, secondAngle) * DegreesToRadians;
            body.LimitMax = Math.Max(firstAngle, secondAngle) * DegreesToRadians;

            Matrix4x4 entityWorld = Matrix4x4.CreateTranslation(body.CenterOffset) * frameWorld;

            if (body.IsDynamic)
            {
                gravityPerRadius = Math.Max(gravityPerRadius, Math.Abs(gravityFactor) / Math.Max(MinimumShapeSize, shape.MaximumRadius));
                float mass = scdBody.Mass;

                //Positive mass is scaled by the bone scale. Negative mass is a density for the shape volume.
                if (mass > 0f)
                    mass *= scale.X * scale.Y * scale.Z;
                else
                    mass = (mass < 0f ? -mass : 1f) * shape.Volume;

                body.Entity = new Entity(shape, mass);

                SimdVector3 inertiaScale = ToSimd(scdBody.InertiaMultipliers);
                BEPUutilities.Matrix3x3 inertia = body.Entity.LocalInertiaTensor;
                if (inertiaScale.X > 0f) inertia.M11 *= inertiaScale.X;
                if (inertiaScale.Y > 0f) inertia.M22 *= inertiaScale.Y;
                if (inertiaScale.Z > 0f) inertia.M33 *= inertiaScale.Z;
                body.Entity.LocalInertiaTensor = inertia;

                body.Entity.LinearDamping = ToBepuDamping(linearDamping);
                body.Entity.AngularDamping = ToBepuDamping(angularDamping);
                body.Drag = scdBody.MovementDrag;
                body.IdleDamping = idleDamping;
            }
            else
            {
                body.Entity = new Entity(shape);
            }

            body.Entity.Material = new BEPUphysics.Materials.Material(Math.Max(0f, staticFriction), Math.Max(0f, dynamicFriction), Math.Max(0f, restitution));
            body.Entity.WorldTransform = ToBepu(entityWorld);
            body.BoneFromEntity = boneWorld[boneIndex] * Invert(entityWorld);

            if (bodyByBone[boneIndex] != null)
                Warnings.Add($"SCD: bone \"{boneName}\" has more than one body. The last one is used for the bone.");

            bodyByBone[boneIndex] = body;
            bodies.Add(body);
            space.Add(body.Entity);
        }

        private static bool IsMotionDynamic(int motionValue, bool isVersion1)
        {
            if (isVersion1)
                return motionValue != 4 && motionValue != 5;

            return motionValue == 2;
        }

        private static ConvexShape CreateScdShape(SCD_ShapeType shapeType, float[] dims, float convexRadius, SimdVector3 scale, out float centerY)
        {
            centerY = 0f;

            switch (shapeType)
            {
                case SCD_ShapeType.Box:
                    return CreateRoundedBox(dims[0] * scale.X, dims[1] * scale.Y, dims[2] * scale.Z, convexRadius * Math.Min(scale.X, Math.Min(scale.Y, scale.Z)));
                case SCD_ShapeType.Sphere:
                    return new SphereShape(Math.Max(MinimumShapeSize, dims[0] * (scale.X + scale.Y + scale.Z) / 3f));
                case SCD_ShapeType.ConvexHull:
                    {
                        //Rounded cylinder around the Y segment from dims[5] to dims[2], approximated as a capsule with the convex radius.
                        centerY = (dims[2] + dims[5]) * 0.5f;
                        float length = Math.Abs(dims[2] - dims[5]) * scale.Y;
                        return new CapsuleShape(length, Math.Max(MinimumShapeSize, convexRadius));
                    }
                case SCD_ShapeType.Capsule:
                    {
                        float radius = dims[0] * (scale.X + scale.Z) * 0.5f;
                        float segment = Math.Abs(dims[2] - dims[5]);
                        float scaledLength = (dims[0] * 2f + segment) * scale.Y - radius * 2f;
                        centerY = (dims[2] + dims[5]) * 0.5f;

                        //A capsule too short for its radius becomes a sphere at the segment center.
                        if (scaledLength <= 0f)
                            return new SphereShape(Math.Max(MinimumShapeSize, dims[0] * scale.Y));

                        if (segment <= 0f)
                            return new SphereShape(Math.Max(MinimumShapeSize, radius));

                        return new CapsuleShape(scaledLength, Math.Max(MinimumShapeSize, radius));
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// The box shrinks by min(radius, a quarter of its smallest side). Then the full radius rounds it outward, so a box thinner than the radius grows by the difference.
        /// </summary>
        private static ConvexShape CreateRoundedBox(float halfX, float halfY, float halfZ, float radius)
        {
            float shrink = Math.Min(radius, Math.Max(0f, BoxShrinkFraction * 2f * Math.Min(halfX, Math.Min(halfY, halfZ))));
            float grow = radius - shrink;
            return CreateBox(halfX + grow, halfY + grow, halfZ + grow, radius);
        }

        private static ConvexShape CreateBox(float halfX, float halfY, float halfZ, float convexRadius)
        {
            halfX = Math.Max(MinimumShapeSize, halfX);
            halfY = Math.Max(MinimumShapeSize, halfY);
            halfZ = Math.Max(MinimumShapeSize, halfZ);

            BoxShape box = new BoxShape(halfX * 2f, halfY * 2f, halfZ * 2f);
            float smallestHalf = Math.Min(halfX, Math.Min(halfY, halfZ));
            box.CollisionMargin = Math.Min(Math.Max(convexRadius, 0.001f), smallestHalf * 0.9f);
            return box;
        }

        private void AddDynamicCollisionRules()
        {
            //SCD dynamic bodies only collide with bone-driven bodies and colliders, not with each other.
            for (int first = 0; first < bodies.Count; first++)
            {
                if (!bodies[first].IsDynamic) continue;

                for (int second = first + 1; second < bodies.Count; second++)
                {
                    if (bodies[second].IsDynamic)
                        CollisionRules.AddRule(bodies[first].Entity, bodies[second].Entity, CollisionRule.NoBroadPhase);
                }
            }
        }

        private void AddJoint(string tag, SCD_Constraint constraint)
        {
            ScdBody bodyA = FindBody(constraint.BoneA);
            ScdBody bodyB = FindBody(constraint.BoneB);

            if (bodyA == null || bodyB == null)
                return;

            SimdVector3 pivotA = SimdVector3.Transform(ToSimd(constraint.PivotA) * bodyA.Scale, bodyA.FrameWorld);
            SimdVector3 pivotB = SimdVector3.Transform(ToSimd(constraint.PivotB) * bodyB.Scale, bodyB.FrameWorld);
            Entity entityA = bodyA.Entity;
            Entity entityB = bodyB.Entity;

            switch (constraint.ConstraintType)
            {
                case SCD_ConstraintType.BallAndSocket:
                    space.Add(CreateBallSocket(entityA, entityB, pivotA, pivotB));
                    break;
                case SCD_ConstraintType.StiffSpring:
                    {
                        DistanceJoint joint = new DistanceJoint(entityA, entityB, ToBepu(pivotA), ToBepu(pivotB));
                        float restLength = constraint.RestLength;

                        if (restLength >= 0f)
                            joint.Distance = restLength;

                        space.Add(joint);
                        break;
                    }
                case SCD_ConstraintType.Hinge:
                case SCD_ConstraintType.LimitedHinge:
                    {
                        SimdVector3 axisA = WorldAxis(ToSimd(constraint.AxisA), bodyA);
                        SimdVector3 axisB = WorldAxis(ToSimd(constraint.AxisB), bodyB);
                        RevoluteJoint joint = new RevoluteJoint(entityA, entityB, ToBepu(pivotA), ToBepu(axisA));
                        joint.BallSocketJoint.OffsetB = ToBepu(pivotB) - entityB.Position;
                        joint.AngularJoint.WorldFreeAxisB = ToBepu(axisB);

                        if (constraint.ConstraintType == SCD_ConstraintType.LimitedHinge)
                        {
                            SimdVector3 perpendicularA = Perpendicular(WorldAxis(ToSimd(constraint.PerpendicularA), bodyA), axisA);
                            SimdVector3 perpendicularB = Perpendicular(WorldAxis(ToSimd(constraint.PerpendicularB), bodyB), axisB);
                            float firstAngle = constraint.Angle1 * DegreesToRadians;
                            float secondAngle = constraint.Angle2 * DegreesToRadians;

                            //The SCD range is the turn of A from B around the axis. The limit below measures B from A, so the range is negated.
                            //With this sign, gravity pushes almost every shipped chain into the range end at its rest pose.
                            joint.Limit.IsActive = true;
                            joint.Limit.Basis.SetWorldAxes(ToBepu(axisA), ToBepu(perpendicularA));
                            joint.Limit.TestAxis = ToBepu(perpendicularB);
                            joint.Limit.MinimumAngle = -Math.Max(firstAngle, secondAngle);
                            joint.Limit.MaximumAngle = -Math.Min(firstAngle, secondAngle);

                            AddHingeFriction(joint, constraint.Friction);
                        }

                        space.Add(joint);
                        break;
                    }
                case SCD_ConstraintType.Ragdoll:
                    {
                        SimdVector3 axisA = WorldAxis(ToSimd(constraint.AxisA), bodyA);
                        SimdVector3 axisB = WorldAxis(ToSimd(constraint.AxisB), bodyB);
                        SimdVector3 perpendicularA = Perpendicular(WorldAxis(ToSimd(constraint.PerpendicularA), bodyA), axisA);
                        SimdVector3 perpendicularB = Perpendicular(WorldAxis(ToSimd(constraint.PerpendicularB), bodyB), axisB);
                        float firstTwist = constraint.Angle1 * DegreesToRadians;
                        float secondTwist = constraint.Angle2 * DegreesToRadians;
                        float coneAngle = Math.Abs(constraint.RagdollAngle3) * DegreesToRadians;
                        float planeMin = Math.Min(constraint.RagdollAngle1, constraint.RagdollAngle2) * DegreesToRadians;
                        float planeMax = Math.Max(constraint.RagdollAngle1, constraint.RagdollAngle2) * DegreesToRadians;

                        space.Add(CreateBallSocket(entityA, entityB, pivotA, pivotB));
                        space.Add(new SwingLimit(entityA, entityB, ToBepu(axisA), ToBepu(axisB), coneAngle));
                        //The native plane limit compares A's twist axis with B's perpendicular axis.
                        space.Add(new SwingLimit(entityA, entityB, ToBepu(axisA), ToBepu(perpendicularB), (float)Math.PI * 0.5f - planeMin));
                        space.Add(new SwingLimit(entityA, entityB, ToBepu(-axisA), ToBepu(perpendicularB), (float)Math.PI * 0.5f + planeMax));
                        TwistLimit twist = new TwistLimit(entityA, entityB, ToBepu(axisA), ToBepu(axisB), Math.Min(firstTwist, secondTwist), Math.Max(firstTwist, secondTwist));
                        twist.BasisA.SetWorldAxes(ToBepu(axisA), ToBepu(perpendicularA), ToBepu(SimdVector3.Cross(axisA, perpendicularA)));
                        twist.BasisB.SetWorldAxes(ToBepu(axisB), ToBepu(perpendicularB));
                        space.Add(twist);
                        break;
                    }
                default:
                    Warnings.Add($"SCD: {tag} between \"{bodyA.BoneName}\" and \"{bodyB.BoneName}\" has unsupported type {(int)constraint.ConstraintType} and was skipped.");
                    return;
            }

            CollisionRules.AddRule(entityA, entityB, CollisionRule.NoBroadPhase);
            JointCount++;
        }

        private static BallSocketJoint CreateBallSocket(Entity entityA, Entity entityB, SimdVector3 pivotA, SimdVector3 pivotB)
        {
            return new BallSocketJoint(entityA, entityB, ToBepu(pivotA))
            {
                OffsetB = ToBepu(pivotB) - entityB.Position
            };
        }

        private static void AddHingeFriction(RevoluteJoint joint, float friction)
        {
            if (friction <= 0f)
                return;

            joint.Motor.IsActive = true;
            joint.Motor.Settings.Mode = MotorMode.VelocityMotor;
            joint.Motor.Settings.VelocityMotor.GoalVelocity = 0f;
            joint.Motor.Settings.MaximumForce = friction;
        }

        private void AddSpring(SCD_Spring scdSpring)
        {
            ScdBody bodyA = FindBody(scdSpring.BoneA);
            ScdBody bodyB = FindBody(scdSpring.BoneB);

            if (bodyA == null || bodyB == null)
                return;

            if (scdSpring.I_16 != 0)
            {
                Warnings.Add($"SCD: ACT0 between \"{bodyA.BoneName}\" and \"{bodyB.BoneName}\" has an unknown type and was skipped.");
                return;
            }

            //Strength and damping scale with the mean bone volume scale of the two bodies.
            float volumeScale = (bodyA.Scale.X * bodyA.Scale.Y * bodyA.Scale.Z + bodyB.Scale.X * bodyB.Scale.Y * bodyB.Scale.Z) * 0.5f;

            SpringLink spring = new SpringLink()
            {
                BodyA = bodyA,
                BodyB = bodyB,
                LocalPivotA = ToSimd(scdSpring.PivotA) * bodyA.Scale - bodyA.CenterOffset,
                LocalPivotB = ToSimd(scdSpring.PivotB) * bodyB.Scale - bodyB.CenterOffset,
                Strength = scdSpring.Strength * volumeScale,
                Damping = scdSpring.Damping * volumeScale,
                OnCompression = (scdSpring.Flags & 1) != 0,
                OnExtension = (scdSpring.Flags & 2) != 0
            };

            float restLength = scdSpring.RestLength;

            if (restLength >= 0f)
            {
                spring.RestLength = restLength * (bodyA.Scale.X + bodyA.Scale.Y + bodyA.Scale.Z + bodyB.Scale.X + bodyB.Scale.Y + bodyB.Scale.Z) / 6f;
            }
            else
            {
                spring.GetWorldPivots(out SimdVector3 pivotA, out SimdVector3 pivotB);
                spring.RestLength = SimdVector3.Distance(pivotA, pivotB);
            }

            springs.Add(spring);
        }

        private void AddCollider(BcsBone bcsBone, int boneSlot, Matrix4x4 boneWorld)
        {
            ConvexShape shape = CreateBcsColliderShape(bcsBone, boneWorld);
            if (shape == null)
            {
                Warnings.Add($"BCS: skeleton entry for \"{bcsBone.BoneName}\" has unsupported shape type {(short)(bcsBone.I_04 & 0xFFFF)} and was skipped.");
                return;
            }

            BoneCollider collider = new BoneCollider()
            {
                BoneName = bcsBone.BoneName,
                BoneSlot = boneSlot,
                ShapeLocal = GetBcsColliderLocal(bcsBone),
                Entity = new Entity(shape)
            };

            collider.Entity.Material = new BEPUphysics.Materials.Material(0.5f, 0.5f, 0f);
            collider.Entity.WorldTransform = ToBepu(collider.GetEntityWorld(boneWorld));
            colliders.Add(collider);
            space.Add(collider.Entity);
        }

        internal static ConvexShape CreateBcsColliderShape(BcsBone bone, Matrix4x4 boneWorld)
        {
            RemoveScale(boneWorld, out SimdVector3 scale);
            float meanScale = (scale.X + scale.Y + scale.Z) / 3f;
            switch ((short)(bone.I_04 & 0xFFFF))
            {
                case 0:
                    return CreateRoundedBox(bone.F_36 * 0.5f * scale.X, bone.F_40 * 0.5f * scale.Y, bone.F_44 * 0.5f * scale.Z, BcsConvexRadius);
                case 1:
                    return new SphereShape(Math.Max(MinimumShapeSize, bone.F_36 * meanScale));
                case 2:
                    return new CylinderShape(Math.Max(MinimumShapeSize, bone.F_40 * meanScale), Math.Max(MinimumShapeSize, bone.F_36 * meanScale));
                case 3:
                    return new CapsuleShape(Math.Max(0f, bone.F_40 * meanScale), Math.Max(MinimumShapeSize, bone.F_36 * meanScale));
                default:
                    return null;
            }
        }

        private static Matrix4x4 GetBcsColliderLocal(BcsBone bone)
        {
            Matrix4x4 local = Matrix4x4.CreateRotationX(bone.F_24) * Matrix4x4.CreateRotationY(bone.F_28) * Matrix4x4.CreateRotationZ(bone.F_32);
            local.Translation = new SimdVector3(bone.F_12, bone.F_16, bone.F_20);
            return local;
        }

        internal static Matrix4x4 GetBcsColliderWorld(BcsBone bone, Matrix4x4 boneWorld)
            => RemoveScale(GetBcsColliderLocal(bone) * boneWorld, out _);

        private ScdBody FindBody(string boneName)
        {
            foreach (ScdBody body in bodies)
            {
                if (body.BoneName == boneName)
                    return body;
            }

            return null;
        }
        #endregion
    }
}
