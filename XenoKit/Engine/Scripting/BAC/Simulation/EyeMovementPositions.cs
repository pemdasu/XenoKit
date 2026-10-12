using Microsoft.Xna.Framework;
using System.IO;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Scripting.BAC.Simulation
{
    public static class EyeMovementPositions
    {
        private static readonly Vector4[] eyePositions =
        {
            new Vector4(-0.15f, -0.03f, -0.04f, -0.0015f),
            new Vector4(0, -0.025f, 0, -0.025f),
            new Vector4(0.04f, -0.0015f, 0.15f, -0.03f),
            new Vector4(-0.15f, 0, -0.04f, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0.04f, 0, 0.15f, 0),
            new Vector4(-0.15f, 0.07f, -0.04f, 0.08f),
            new Vector4(0, 0.07f, 0, 0.07f),
            new Vector4(0.04f, 0.08f, 0.15f, 0.07f)
        };

        public static Vector4 GetPosition(BAC_Type21 movement, float elapsedFrames, Vector2 partScale)
        {
            int previous = (int)movement.EyeDirectionPrev;
            int next = (int)movement.EyeDirectionNext;
            if (previous >= eyePositions.Length || next >= eyePositions.Length)
                throw new InvalidDataException($"Eye movement directions must be between 0 and 8 (previous: {previous}, next: {next}).");

            // These stored fields are the previous and next direction strengths, not separate eye strengths.
            Vector4 start = eyePositions[previous] * movement.LeftEyeRotationPercent;
            Vector4 end = eyePositions[next] * movement.RightEyeRotationPercent;
            float factor = movement.EyeRotationFrames > 0 ? MathHelper.Clamp(elapsedFrames / movement.EyeRotationFrames, 0, 1) : 1;
            Vector4 position = Vector4.Lerp(start, end, factor);
            float horizontalScale = partScale.X > 0 ? partScale.X * 1.25f : 1;
            float verticalScale = partScale.Y > 0 ? partScale.Y * 1.42857146f : 1;
            return new Vector4(position.X * horizontalScale, position.Y * verticalScale,
                position.Z * horizontalScale, position.W * verticalScale);
        }
    }
}
