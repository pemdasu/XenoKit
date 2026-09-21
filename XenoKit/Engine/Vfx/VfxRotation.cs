using System;
using System.Numerics;
using XenoKit.Helper;

namespace XenoKit.Engine.Vfx
{
    public static class VfxRotation
    {
        public static Matrix4x4 Create(float rotationX, float rotationY, float rotationZ)
        {
            return Matrix4x4.CreateFromQuaternion(GeneralHelpers.EulerAnglesToQuaternion(new Vector3(rotationX, rotationY, rotationZ)));
        }

        public static Vector3 GetDirectionAxis(byte direction)
        {
            switch (direction)
            {
                case 0: return Vector3.UnitX;
                case 1: return Vector3.UnitY;
                case 2: return -Vector3.UnitZ;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown effect direction axis.");
            }
        }

        public static Matrix4x4 CreateUser(byte directionAxis, Vector3 direction)
        {
            // The game starts unknown User directions from +Y without a half-turn.
            if (directionAxis > 2)
                return AlignDirection(Vector3.UnitY, direction);
            Vector3 source = GetDirectionAxis(directionAxis);
            if (Vector3.Dot(source, direction) >= 0f)
                return AlignDirection(source, direction);

            Matrix4x4 turn;
            switch (directionAxis)
            {
                case 0:
                case 2:
                    turn = Matrix4x4.CreateRotationY((float)Math.PI);
                    break;
                case 1:
                    turn = Matrix4x4.CreateRotationZ((float)Math.PI);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(directionAxis));
            }
            return turn * AlignDirection(-source, direction);
        }

        public static Matrix4x4 CreateMovement(byte directionAxis, Vector3 displacement, ref Vector3 direction)
        {
            Vector3 source = directionAxis > 2 ? Vector3.Zero : GetDirectionAxis(directionAxis);
            displacement.Y = 0f;
            float blend = Math.Max(0f, 1f - 2f * displacement.Length());
            Vector3 movement = Vector3.Lerp(displacement, source, blend);
            Vector3 normal = movement == Vector3.Zero ? Vector3.Zero : Vector3.Normalize(movement);
            float weight = Math.Max(0f, Math.Min(1f, Vector3.Dot(normal, direction) + 0.133f));
            direction += movement * weight;
            if (direction != Vector3.Zero)
                direction = Vector3.Normalize(direction);
            return AlignDirection(source, direction);
        }

        public static Matrix4x4 CreateCamera(Matrix4x4 view)
        {
            if (!Matrix4x4.Invert(view, out Matrix4x4 camera))
                throw new InvalidOperationException("Cannot orient an effect with a singular camera view matrix.");
            camera.Translation = Vector3.Zero;
            return camera;
        }

        private static Matrix4x4 AlignDirection(Vector3 source, Vector3 direction)
        {
            // The game uses the normalized halfway vector and keeps its zero-vector result.
            Vector3 halfway = source + direction;
            if (halfway != Vector3.Zero)
                halfway = Vector3.Normalize(halfway);
            return Matrix4x4.CreateFromQuaternion(new Quaternion(Vector3.Cross(source, halfway), Vector3.Dot(source, halfway)));
        }
    }
}
