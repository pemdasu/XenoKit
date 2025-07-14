using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Xv2CoreLib.EMD;
using Xv2CoreLib.FMP;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine
{
    public static class Extensions
    {
        public static Quaternion EulerToQuaternion(this Vector3 euler)
        {
            float roll = MathHelpers.ToRadians(euler.X);
            float pitch = MathHelpers.ToRadians(euler.Y);
            float yaw = MathHelpers.ToRadians(euler.Z);

            float cy = (float)Math.Cos(yaw * 0.5f);
            float sy = (float)Math.Sin(yaw * 0.5f);
            float cp = (float)Math.Cos(pitch * 0.5f);
            float sp = (float)Math.Sin(pitch * 0.5f);
            float cr = (float)Math.Cos(roll * 0.5f);
            float sr = (float)Math.Sin(roll * 0.5f);

            return new Quaternion(
                sr * cp * cy - cr * sp * sy,
                cr * sp * cy + sr * cp * sy,
                cr * cp * sy - sr * sp * cy,
                cr * cp * cy + sr * sp * sy
            );
        }

        public static Matrix ToXna(this FMP_Matrix matrix)
        {
            return new Matrix(matrix.L0[0], matrix.L0[1], matrix.L0[2], 0f,
                              matrix.L1[0], matrix.L1[1], matrix.L1[2], 0f,
                              matrix.L2[0], matrix.L2[1], matrix.L2[2], 0f,
                              matrix.L3[0], matrix.L3[1], matrix.L3[2], 1f);

        }
        
        public static BoundingBox ConvertToBoundingBox(this EMD_AABB aabb)
        {
            Vector3 min = new Vector3(aabb.MinX, aabb.MinY, aabb.MinZ);
            Vector3 max = new Vector3(aabb.MaxX, aabb.MaxY, aabb.MaxZ);
            return new BoundingBox(min, max);
        }

        public static BoundingBox Transform(this BoundingBox box, Matrix transform)
        {
            Vector3[] corners = box.GetCorners();
            Vector3 min = Vector3.Transform(corners[0], transform);
            Vector3 max = min;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 transformed = Vector3.Transform(corners[i], transform);
                min = Vector3.Min(min, transformed);
                max = Vector3.Max(max, transformed);
            }

            return new BoundingBox(min, max);
        }

        #region System.Numerics Conversion
        //Extension methods for converting between MonoGame and System.Numerics types

        public static Vector2 ToXna(this System.Numerics.Vector2 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static System.Numerics.Vector2 ToNumerics(this Vector2 vector)
        {
            return new System.Numerics.Vector2(vector.X, vector.Y);
        }

        public static Vector3 ToXna(this System.Numerics.Vector3 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static System.Numerics.Vector3 ToNumerics(this Vector3 vector)
        {
            return new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vector4 ToXna(this System.Numerics.Vector4 vector)
        {
            return new Vector4(vector.X, vector.Y, vector.Z, vector.W);
        }

        public static System.Numerics.Vector4 ToNumerics(this Vector4 vector)
        {
            return new System.Numerics.Vector4(vector.X, vector.Y, vector.Z, vector.W);
        }
        
        public static Quaternion ToXna(this System.Numerics.Quaternion quaternion)
        {
            return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        public static System.Numerics.Quaternion ToNumerics(this Quaternion quaternion)
        {
            return new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        public static Matrix ToXna(System.Numerics.Matrix4x4 matrix)
        {
            return new Matrix(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }

        public static System.Numerics.Matrix4x4 ToNumerics(Matrix matrix)
        {
            return new System.Numerics.Matrix4x4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }

        #endregion

        #region Vector3Helpers
        public static bool IsAproxEqual(this Vector3 a, Vector3 b)
        {
            return MathHelpers.FloatEquals(a.X, b.X) && MathHelpers.FloatEquals(a.Y, b.Y) && MathHelpers.FloatEquals(a.Z, b.Z);
        }

        public static void ClampScale(this Vector3 scale)
        {
            scale.Clamp(0.01f, float.MaxValue);
        }

        public static void Clamp(this Vector3 a, float min, float max)
        {
            a.X = MathHelper.Clamp(a.X, min, max);
            a.Y = MathHelper.Clamp(a.Y, min, max);
            a.Z = MathHelper.Clamp(a.Z, min, max);
        }

        public static void ClampEuler(this Vector3 euler)
        {
            if (euler.X > 360)
                euler.X -= 360;
            if (euler.Y > 360)
                euler.Y -= 360;
            if (euler.Z > 360)
                euler.Z -= 360;

            if (euler.X < 0f)
                euler.X += 360;
            if (euler.Y < 0f)
                euler.Y += 360;
            if (euler.Z < 0f)
                euler.Z += 360;
        }

        public static Vector3 GetCenter(this EMD_AABB aabb)
        {
            return new Vector3(aabb.CenterX, aabb.CenterY, aabb.CenterZ) * aabb.CenterW;
        }

        public static Vector3 ToEuler(this Quaternion q)
        {
            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            float roll = (float)Math.Atan2(sinr_cosp, cosr_cosp); // X-axis

            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            float pitch = (float)(Math.Abs(sinp) >= 1 ? MathHelpers.CopySign(MathHelpers.PI / 2, sinp) : Math.Asin(sinp)); // Y-axis

            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            float yaw = (float)Math.Atan2(siny_cosp, cosy_cosp); // Z-axis

            return new Vector3(roll, pitch, yaw) * (180f / MathHelpers.PI); // Convert to degrees
        }

        #endregion

        #region ShaderHelpers
        public static void CopyState(this BlendState blend, int copyFrom, int copyTo)
        {
            if (copyFrom < 0 || copyFrom >= 4)
                throw new ArgumentOutOfRangeException("BlendState.CopyState: copyFrom param out of range.");

            if (copyTo < 0 || copyTo >= 4)
                throw new ArgumentOutOfRangeException("BlendState.CopyState: copyTo param out of range.");

            blend[copyTo].AlphaBlendFunction = blend[copyFrom].AlphaBlendFunction;
            blend[copyTo].AlphaSourceBlend = blend[copyFrom].AlphaSourceBlend;
            blend[copyTo].AlphaDestinationBlend = blend[copyFrom].AlphaDestinationBlend;

            blend[copyTo].ColorBlendFunction = blend[copyFrom].ColorBlendFunction;
            blend[copyTo].ColorSourceBlend = blend[copyFrom].ColorSourceBlend;
            blend[copyTo].ColorDestinationBlend = blend[copyFrom].ColorDestinationBlend;

            blend[copyTo].ColorWriteChannels = blend[copyFrom].ColorWriteChannels;
        }

        public static void ApplyAlphaBlend(this BlendState blendState, int applyTo)
        {
            blendState[applyTo].ColorSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].ColorDestinationBlend = Blend.InverseSourceAlpha;
            blendState[applyTo].ColorBlendFunction = BlendFunction.Add;

            blendState[applyTo].AlphaSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].AlphaDestinationBlend = Blend.InverseSourceAlpha;
            blendState[applyTo].AlphaBlendFunction = BlendFunction.Add;

            blendState[applyTo].ColorWriteChannels = ColorWriteChannels.All;
        }

        public static void ApplyAdditive(this BlendState blendState, int applyTo)
        {
            blendState[applyTo].ColorSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].ColorDestinationBlend = Blend.One;
            blendState[applyTo].ColorBlendFunction = BlendFunction.Add;

            blendState[applyTo].AlphaSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].AlphaDestinationBlend = Blend.One;
            blendState[applyTo].AlphaBlendFunction = BlendFunction.Add;

            blendState[applyTo].ColorWriteChannels = ColorWriteChannels.All;
        }

        public static void ApplySubtractive(this BlendState blendState, int applyTo)
        {
            blendState[applyTo].AlphaBlendFunction = BlendFunction.ReverseSubtract;
            blendState[applyTo].AlphaSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].AlphaDestinationBlend = Blend.One;

            blendState[applyTo].ColorBlendFunction = BlendFunction.ReverseSubtract;
            blendState[applyTo].ColorSourceBlend = Blend.SourceAlpha;
            blendState[applyTo].ColorDestinationBlend = Blend.One;

            blendState[applyTo].ColorWriteChannels = ColorWriteChannels.All;
        }

        public static void ApplyCustom(this BlendState blendState, int applyTo, BlendFunction blendFunction, Blend sourceBlend, Blend destinationBlend, ColorWriteChannels colorMask = ColorWriteChannels.All)
        {
            blendState[applyTo].AlphaBlendFunction = blendFunction;
            blendState[applyTo].AlphaSourceBlend = sourceBlend;
            blendState[applyTo].AlphaDestinationBlend = destinationBlend;

            blendState[applyTo].ColorBlendFunction = blendFunction;
            blendState[applyTo].ColorSourceBlend = sourceBlend;
            blendState[applyTo].ColorDestinationBlend = destinationBlend;

            blendState[applyTo].ColorWriteChannels = colorMask;
        }

        public static void ApplyNone(this BlendState blendState, int applyTo)
        {
            blendState[applyTo].ColorSourceBlend = Blend.One;
            blendState[applyTo].ColorDestinationBlend = Blend.Zero;
            blendState[applyTo].ColorBlendFunction = BlendFunction.Add;

            blendState[applyTo].AlphaSourceBlend = Blend.One;
            blendState[applyTo].AlphaDestinationBlend = Blend.Zero;
            blendState[applyTo].AlphaBlendFunction = BlendFunction.Add;

            blendState[applyTo].ColorWriteChannels = ColorWriteChannels.All;
        }
        #endregion
    }
}