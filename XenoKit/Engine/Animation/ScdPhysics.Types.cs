using BEPUphysics.Entities;
using System;
using Xv2CoreLib.SCD;
using BepuMatrix = BEPUutilities.Matrix;
using BepuVector3 = BEPUutilities.Vector3;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Animation
{
    public sealed partial class ScdPhysics
    {
        #region Helpers
        private static SimdVector3 ToSimd(SCD_Vector3 vector) => new SimdVector3(vector.X, vector.Y, vector.Z);

        private static float ClampAngle(float degrees)
        {
            return Math.Max(-180f, Math.Min(180f, degrees));
        }

        private static float ToBepuDamping(float damping)
        {
            //Converts a damping rate into the fraction of velocity removed per second.
            return damping > 0f ? Math.Min(1f, 1f - (float)Math.Exp(-damping)) : 0f;
        }

        private static SimdVector3 WorldAxis(SimdVector3 localAxis, ScdBody body)
        {
            return SafeNormalize(SimdVector3.TransformNormal(localAxis, body.FrameWorld));
        }

        private static SimdVector3 Perpendicular(SimdVector3 vector, SimdVector3 axis)
        {
            return SafeNormalize(vector - axis * SimdVector3.Dot(vector, axis));
        }

        private static SimdVector3 SafeNormalize(SimdVector3 vector)
        {
            float length = vector.Length();
            return length > 1e-6f ? vector / length : SimdVector3.Zero;
        }

        private static Matrix4x4 RemoveScale(Matrix4x4 matrix, out SimdVector3 scale)
        {
            SimdVector3 row1 = new SimdVector3(matrix.M11, matrix.M12, matrix.M13);
            SimdVector3 row2 = new SimdVector3(matrix.M21, matrix.M22, matrix.M23);
            SimdVector3 row3 = new SimdVector3(matrix.M31, matrix.M32, matrix.M33);
            scale = new SimdVector3(row1.Length(), row2.Length(), row3.Length());

            row1 = SafeNormalize(row1);
            row2 = SafeNormalize(row2);
            row3 = SafeNormalize(row3);

            return new Matrix4x4(row1.X, row1.Y, row1.Z, 0f,
                                 row2.X, row2.Y, row2.Z, 0f,
                                 row3.X, row3.Y, row3.Z, 0f,
                                 matrix.M41, matrix.M42, matrix.M43, 1f);
        }

        //Setup only. Per-frame code uses Matrix4x4.Invert and keeps the animated pose when it fails (hidden parts can have zero scale).
        private static Matrix4x4 Invert(Matrix4x4 matrix)
        {
            if (!Matrix4x4.Invert(matrix, out Matrix4x4 inverse))
                throw new InvalidOperationException("ScdPhysics: a bone matrix could not be inverted.");

            return inverse;
        }

        private static BepuVector3 ToBepu(SimdVector3 vector) => new BepuVector3(vector.X, vector.Y, vector.Z);

        private static SimdVector3 ToSimd(BepuVector3 vector) => new SimdVector3(vector.X, vector.Y, vector.Z);

        private static BepuMatrix ToBepu(Matrix4x4 matrix)
        {
            return new BepuMatrix(matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                                  matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                                  matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                                  matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }

        private static Matrix4x4 ToSimd(BepuMatrix matrix)
        {
            return new Matrix4x4(matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                                 matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                                 matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                                 matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }
        #endregion

        #region Types
        private sealed class ScdBody
        {
            public string BoneName;
            public int BoneIndex;
            public Entity Entity;
            public bool IsDynamic;
            public Matrix4x4 ShapeLocal;
            public SimdVector3 CenterOffset;
            public SimdVector3 Scale;
            public Matrix4x4 FrameWorld;
            public Matrix4x4 BoneFromEntity;
            public int Flags;
            public SimdVector3 Pivot;
            public SimdVector3 LimitAxis;
            public float LimitMin;
            public float LimitMax;
            public float Drag;
            public float IdleDamping;
            public float GravityFactor;
            public Matrix4x4 AnimatedEntityWorld;

            public Matrix4x4 GetEntityWorld(Matrix4x4 boneWorld)
            {
                return Matrix4x4.CreateTranslation(CenterOffset) * RemoveScale(ShapeLocal * boneWorld, out _);
            }
        }

        private sealed class BoneCollider
        {
            public string BoneName;
            public int BoneSlot;
            public Matrix4x4 ShapeLocal;
            public Entity Entity;
            public Matrix4x4 AnimatedEntityWorld;

            public Matrix4x4 GetEntityWorld(Matrix4x4 boneWorld)
            {
                return RemoveScale(ShapeLocal * boneWorld, out _);
            }
        }

        private sealed class SpringLink
        {
            public ScdBody BodyA;
            public ScdBody BodyB;
            public SimdVector3 LocalPivotA;
            public SimdVector3 LocalPivotB;
            public float RestLength;
            public float Strength;
            public float Damping;
            public bool OnCompression;
            public bool OnExtension;

            public void GetWorldPivots(out SimdVector3 pivotA, out SimdVector3 pivotB)
            {
                pivotA = SimdVector3.Transform(LocalPivotA, ToSimd(BodyA.Entity.WorldTransform));
                pivotB = SimdVector3.Transform(LocalPivotB, ToSimd(BodyB.Entity.WorldTransform));
            }

            public void Apply(float timeStep)
            {
                GetWorldPivots(out SimdVector3 pivotA, out SimdVector3 pivotB);
                SimdVector3 offset = pivotB - pivotA;
                float length = offset.Length();

                if (length < 1e-6f)
                    return;

                if (length < RestLength ? !OnCompression : !OnExtension)
                    return;

                SimdVector3 direction = offset / length;
                SimdVector3 relativeVelocity = GetPointVelocity(BodyB.Entity, pivotB) - GetPointVelocity(BodyA.Entity, pivotA);
                float force = Strength * (length - RestLength) + Damping * SimdVector3.Dot(relativeVelocity, direction);
                BepuVector3 impulse = ToBepu(direction * (force * timeStep));

                if (BodyA.IsDynamic)
                    BodyA.Entity.ApplyImpulse(ToBepu(pivotA), impulse);

                if (BodyB.IsDynamic)
                    BodyB.Entity.ApplyImpulse(ToBepu(pivotB), -impulse);
            }

            private static SimdVector3 GetPointVelocity(Entity entity, SimdVector3 point)
            {
                return ToSimd(entity.LinearVelocity) + SimdVector3.Cross(ToSimd(entity.AngularVelocity), point - ToSimd(entity.Position));
            }
        }
        #endregion
    }
}
