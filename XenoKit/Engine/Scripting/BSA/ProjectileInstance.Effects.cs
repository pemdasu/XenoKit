using System;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Vfx;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    public partial class ProjectileInstance
    {
        private void EndEffects(bool force)
        {
            foreach (ActiveProjectileEffect effect in activeEffects)
                effect.Effect?.Terminate(force);

            activeEffects.Clear();
        }

        private void PlayDueEffects(float previousFrame, float targetFrame)
        {
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation) return;
            if (actor == null) return;

            foreach (BSA_Type6 effect in bsaEntry.IBsaTypes?.OfType<BSA_Type6>() ?? Enumerable.Empty<BSA_Type6>())
            {
                if (playedEffects.Contains(effect) || !IsEffectDue(effect, previousFrame, targetFrame))
                    continue;

                if (((ushort)effect.I_08 & 1) != 0)
                    StopEffect(effect);
                else
                    PlayEffect(effect, GetProjectileTransformAtFrame(effect.StartTime));

                playedEffects.Add(effect);
            }
        }

        private void PlayDueSounds(float previousFrame, float targetFrame)
        {
            foreach (BSA_Type7 sound in sounds)
            {
                if (playedSounds.Contains(sound) || !IsTimedTypeDue(sound, previousFrame, targetFrame))
                    continue;

                playedSounds.Add(sound);

                if (actor == null || Viewport.Instance?.IsPlaying != true || sound.CueId < 0 || (ushort)sound.CueId == ushort.MaxValue ||
                    (sound.I_02 & 0x2000) != 0)
                    continue;

                if (!BsaSoundResources.TryGetBacAcbType(sound.AcbType, out Xv2CoreLib.BAC.AcbType acbType))
                    continue;

                Xv2CoreLib.ACB.ACB_Wrapper acb = Files.Instance.GetAcbFile(acbType, move, actor, true);
                if (acb != null)
                    Viewport.Instance.AudioEngine.PlayCue((ushort)sound.CueId, acb, this);
            }
        }

        private void PlayDueScreenEffects(float previousFrame, float targetFrame)
        {
            foreach (BSA_Type8 screenEffect in screenEffects)
            {
                if (playedScreenEffects.Contains(screenEffect) || !IsTimedTypeDue(screenEffect, previousFrame, targetFrame))
                    continue;

                playedScreenEffects.Add(screenEffect);
                Xv2CoreLib.BPE.BPE_Entry bpeEntry = Files.Instance.GetBpeEntry(screenEffect.I_00, true);
                if (bpeEntry == null)
                    continue;

                if (bacInstance != null)
                    bacInstance.StartScreenEffect(bpeEntry);
                else
                    BsaEffectPreviewController.Instance.StartScreenEffect(bpeEntry);
            }
        }

        private void PlayEffect(BSA_Type6 effect, Matrix4x4 eventProjectileTransform)
        {
            Matrix4x4 offset = CreateEffectOffset(effect);
            Matrix4x4 effectTransform = ApplyEffectOffset(GetEffectRootTransform(eventProjectileTransform), offset);
            Matrix4x4 nodeTransform = ApplyEffectOffset(eventProjectileTransform, offset);
            VfxEffect vfxEffect = actor.VfxManager.PlayProjectileEffect(effect, move, actor, effectTransform, nodeTransform);

            if (vfxEffect != null)
            {
                activeEffects.Add(new ActiveProjectileEffect(effect, vfxEffect, offset, currentFrame));
            }
        }

        private static Matrix4x4 CreateEffectOffset(BSA_Type6 effect)
        {
            return Matrix4x4.CreateTranslation(new SimdVector3(effect.F_12, effect.F_16, effect.F_20));
        }

        private static Matrix4x4 ApplyEffectOffset(Matrix4x4 projectileTransform, Matrix4x4 effectOffset)
        {
            projectileTransform.Translation += effectOffset.Translation;
            return projectileTransform;
        }

        private Matrix4x4 GetEffectRootTransform(Matrix4x4 projectileTransform)
        {
            projectileTransform.Translation = effectRootPosition;
            return projectileTransform;
        }

        private void StopEffect(BSA_Type6 effect)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (!activeEffects[i].Matches(effect))
                    continue;

                activeEffects[i].Effect?.Terminate(false);
                activeEffects.RemoveAt(i);
            }

            parent?.StopEffect(effect);
        }

        private void UpdateActiveEffectTransforms()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].Effect == null || activeEffects[i].Effect.IsDestroyed)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }

                if (activeEffects[i].SkipTransformUpdateOnCreatedFrame && activeEffects[i].CreatedFrame == currentFrame)
                {
                    activeEffects[i].SkipTransformUpdateOnCreatedFrame = false;
                    continue;
                }

                Matrix4x4 effectTransform = ApplyEffectOffset(GetEffectRootTransform(transform), activeEffects[i].Offset);
                Matrix4x4 nodeTransform = ApplyEffectOffset(transform, activeEffects[i].Offset);
                activeEffects[i].Effect.SetExternalTransform(effectTransform, nodeTransform);
            }
        }

        private class ActiveProjectileEffect
        {
            public BSA_Type6 Source { get; }
            public VfxEffect Effect { get; }
            public Matrix4x4 Offset { get; }
            public float CreatedFrame { get; }
            public bool SkipTransformUpdateOnCreatedFrame { get; set; }

            public ActiveProjectileEffect(BSA_Type6 source, VfxEffect effect, Matrix4x4 offset, float createdFrame)
            {
                Source = source;
                Effect = effect;
                Offset = offset;
                CreatedFrame = createdFrame;
                SkipTransformUpdateOnCreatedFrame = true;
            }

            public bool Matches(BSA_Type6 effect)
            {
                return Source.EepkType == effect.EepkType &&
                       Source.SkillID == effect.SkillID &&
                       Source.EffectID == effect.EffectID;
            }
        }
    }
}
