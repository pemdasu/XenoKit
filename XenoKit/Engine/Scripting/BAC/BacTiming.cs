using System;

namespace XenoKit.Engine.Scripting.BAC
{
    /// <summary>
    /// BAC frame timing. BAC frames are fractional.
    /// </summary>
    public static class BacTiming
    {
        public const float NotStartedFrame = -1f;

        /// <summary>
        /// Gets the BAC frame for the next tick from the playing animation.
        /// With no playing animation the frame stays. After the animation ends, the frame runs at one frame per tick.
        /// With no driving Type0 the frame is the animation frame. With one it is StartTime + animation frame - frame offset.
        /// </summary>
        public static float GetNextFrame(float currentFrame, bool hasAnimation, float animationFrame, bool animationFinished, bool hasDrivingAnimation, int drivingStartTime, int drivingFrameOffset, int playbackStartFrame = 0)
        {
            if (currentFrame < 0f)
                return playbackStartFrame;

            if (!hasAnimation)
                return currentFrame;

            if (animationFinished)
                return currentFrame + 1f;

            if (!hasDrivingAnimation)
                return animationFrame;

            return drivingStartTime + animationFrame - drivingFrameOffset;
        }

        /// <summary>
        /// An entry is active when its start time falls in (previousFrame, currentFrame], or when currentFrame is inside its duration.
        /// </summary>
        public static bool IsActive(float startTime, float duration, float previousFrame, float currentFrame)
        {
            if (duration <= 0f)
                return false;

            bool crossedStart = previousFrame < startTime && startTime <= currentFrame;
            bool insideDuration = startTime <= currentFrame && currentFrame < startTime + duration;

            return crossedStart || insideDuration;
        }

        public static float GetCoveredFrames(float startTime, float duration, float previousFrame, float currentFrame)
            => Math.Max(0f, Math.Min(currentFrame, startTime + duration) - Math.Max(previousFrame, startTime));
    }
}
