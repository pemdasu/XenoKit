using System;
using Microsoft.Xna.Framework;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleUV
    {
        public EMP_TextureSamplerDef TextureDef { get; private set; }

        private float CurrentTime = 0f;
        private int CurrentKeyframeIndex = 0;
        private bool IsSpriteSheetComplete;

        public float ScrollU = 0f;
        public float ScrollV = 0f;
        public float StepU = 1f;
        public float StepV = 1f;

        public float TextureStartU => IsUInverted ? ScrollU + StepU : ScrollU;
        public float TextureStartV => IsVInverted ? ScrollV + StepV : ScrollV;
        public float TextureStepU => IsUInverted ? -StepU : StepU;
        public float TextureStepV => IsVInverted ? -StepV : StepV;
        public bool RotateUv => ((TextureDef?.I_02_b ?? 0) & 1) != 0;

        private bool IsUInverted => TextureDef?.SymmetryU == EMP_TextureSamplerDef.SymmetryType.Inverted;
        private bool IsVInverted => TextureDef?.SymmetryV == EMP_TextureSamplerDef.SymmetryType.Inverted;

        internal ParticleUvMap GetMapping()
        {
            return new ParticleUvMap(TextureStartU, TextureStartV, TextureStepU, TextureStepV, RotateUv);
        }

        public ParticleUV() { }

        public ParticleUV(EMP_TextureSamplerDef texture)
        {
            SetTexture(texture);
        }

        public void Update(float frameDelta)
        {
            if (TextureDef == null) return;

            switch (TextureDef.ScrollState.ScrollType)
            {
                case EMP_ScrollState.ScrollTypeEnum.Speed:
                    UpdateScrollSpeed(frameDelta);
                    break;
                case EMP_ScrollState.ScrollTypeEnum.SpriteSheet:
                    UpdateSpriteSheet(frameDelta);
                    break;
            }
        }

        private void UpdateScrollSpeed(float frameDelta)
        {
            float speedU = TextureDef.ScrollState.ScrollSpeed_U;
            float speedV = TextureDef.ScrollState.ScrollSpeed_V;

            if (RotateUv)
            {
                float speed = speedU;
                speedU = speedV;
                speedV = speed;
            }

            ScrollU += (IsUInverted ? -speedU : speedU) * frameDelta;
            ScrollV += (IsVInverted ? -speedV : speedV) * frameDelta;
            WrapScroll(ref ScrollU, StepU);
            WrapScroll(ref ScrollV, StepV);
        }

        private static void WrapScroll(ref float scroll, float step)
        {
            float end = scroll + step;

            if (scroll > 1f && end > 1f)
                scroll -= 1f;
            else if (scroll < -1f && end < -1f)
                scroll += 1f;
        }

        private void UpdateSpriteSheet(float frameDelta)
        {
            var keyframes = TextureDef.ScrollState.Keyframes;
            if (keyframes.Count == 0) return;

            bool isRandom = ((byte)TextureDef.KeyframeType & 0x2) != 0;
            bool isLooping = ((byte)TextureDef.KeyframeType & 0x1) != 0;

            if (!IsSpriteSheetComplete)
            {
                CurrentTime += frameDelta;

                while (true)
                {
                    int keyframeTime = keyframes[CurrentKeyframeIndex].Time;
                    if (keyframeTime <= 0)
                        throw new InvalidOperationException("Sprite-sheet keyframe time must be greater than zero.");

                    if (CurrentTime < keyframeTime)
                        break;

                    CurrentTime -= keyframeTime;

                    if (isRandom)
                    {
                        CurrentKeyframeIndex = Xv2CoreLib.Random.Range(0, keyframes.Count - 1);
                    }
                    else if (CurrentKeyframeIndex < keyframes.Count - 1)
                    {
                        CurrentKeyframeIndex++;
                    }
                    else if (isLooping)
                    {
                        CurrentKeyframeIndex = 0;
                    }
                    else
                    {
                        IsSpriteSheetComplete = true;
                    }

                    if (IsSpriteSheetComplete)
                        break;
                }
            }

            SetKeyframe(keyframes[CurrentKeyframeIndex]);
        }

        private void SetKeyframe(EMP_ScrollKeyframe keyframe)
        {
            ScrollU = keyframe.ScrollU;
            ScrollV = keyframe.ScrollV;
            StepU = keyframe.ScaleU;
            StepV = keyframe.ScaleV;
        }

        public void SetTexture(EMP_TextureSamplerDef texture)
        {
            TextureDef = texture;
            CurrentKeyframeIndex = 0;
            IsSpriteSheetComplete = false;
            CurrentTime = 0;
            ScrollU = 0f;
            ScrollV = 0f;
            StepU = 1f;
            StepV = 1f;

            if (texture?.ScrollState?.ScrollType == EMP_ScrollState.ScrollTypeEnum.Static && texture.ScrollState.Keyframes.Count > 0)
            {
                SetKeyframe(texture.ScrollState.Keyframes[0]);
            }
        }

    }

    internal readonly struct ParticleUvMap
    {
        private readonly float startU;
        private readonly float startV;
        private readonly float stepU;
        private readonly float stepV;
        private readonly bool rotate;

        public ParticleUvMap(float startU, float startV, float stepU, float stepV, bool rotate)
        {
            this.startU = startU;
            this.startV = startV;
            this.stepU = stepU;
            this.stepV = stepV;
            this.rotate = rotate;
        }

        public Vector2 Map(Vector2 coordinate)
        {
            return rotate
                ? new Vector2(startU + coordinate.Y * stepU, startV + coordinate.X * stepV)
                : new Vector2(startU + coordinate.X * stepU, startV + coordinate.Y * stepV);
        }
    }
}
