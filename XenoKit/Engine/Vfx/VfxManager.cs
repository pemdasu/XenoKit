using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Character;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx
{
    /// <summary>
    /// This is the main effect manager class. This can play multiple effects simultaneously, and handles loops and deactivations.
    /// </summary>
    public class VfxManager : EngineObject
    {
        private const int MAX_EFFECTS = 100;
        private List<VfxEffect> Effects = new List<VfxEffect>();

        private List<VfxEffect> NewEffects = new List<VfxEffect>();

        // Effects load off-thread, so the MAX_EFFECTS check counts in-flight loads too.
        private int pendingEffectLoads;
        // Bumped by StopEffects so loads started before the stop are discarded when they finish.
        private int effectGeneration;

        /// <summary>
        /// Force effects to fully update on the next cycle, even if its via Simulate().
        /// </summary>
        public bool ForceEffectUpdate { get; set; }


        #region PlayAndStop
        public void PlayEffect(Effect effect, Actor actor)
        {
            PlayEffect(effect, actor, Matrix4x4.Identity);
        }

        public async void PlayEffect(Effect effect, Actor actor, Matrix4x4 world)
        {
            if (effect == null || actor == null) return;

            if (!TryReserveEffectSlot())
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            int generation = effectGeneration;

            try
            {
                await Task.Run(() => AddEffect(actor, effect, world, generation));
            }
            catch (Exception ex)
            {
                Log.Add($"VfxManager.PlayEffect: could not play effect. {ex.Message}", LogType.Warning);
            }
            finally
            {
                ReleaseEffectSlot();
            }
        }

        public async void PlayEffect(BAC_Type8 bacEffect, BacEntryInstance bacInstance, Actor actor)
        {
            if (!TryReserveEffectSlot())
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            try
            {
                int generation = effectGeneration;
                int skillId = bacEffect.UseSkillId == BAC_Type8.UseSkillIdEnum.True ? bacEffect.SkillID : 0;

                EffectContainerFile eepk = Files.Instance.GetEepkFile(bacEffect.EepkType, (ushort)skillId, bacInstance.SkillMove, bacInstance.User, true);

                if (eepk != null)
                {
                    Effect eepkEffect = eepk.GetEffect(bacEffect.EffectID);

                    if (eepkEffect != null)
                    {
                        //Get spawn position from declared bone and position on the bac entry
                        Matrix4x4 spawnPosition = Matrix4x4.Identity;

                        if (actor != null && (int)bacEffect.BoneLink < 25)
                        {
                            spawnPosition = actor.GetAbsoluteBoneMatrix(actor.Skeleton.BAC_BoneIndices[(int)bacEffect.BoneLink]) * Matrix4x4.CreateTranslation(new SimdVector3(bacEffect.PositionX, bacEffect.PositionY, bacEffect.PositionZ));
                        }

                        await Task.Run(() => AddEffect(bacInstance.User, eepkEffect, spawnPosition, generation));
                    }
                    else
                    {
                        Log.Add($"No effect at ID {bacEffect.EffectID} could be found in EEPK {bacEffect.EepkType}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add($"VfxManager.PlayEffect: could not play effect. {ex.Message}", LogType.Warning);
            }
            finally
            {
                ReleaseEffectSlot();
            }
        }

        public void PlayEffect(DamageManager bdmInstance)
        {
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect1_EepkType, bdmInstance.BdmSubEntry.Effect1_SkillID, bdmInstance.BdmSubEntry.Effect1_ID, bdmInstance);
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect2_EepkType, bdmInstance.BdmSubEntry.Effect2_SkillID, bdmInstance.BdmSubEntry.Effect2_ID, bdmInstance);
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect3_EepkType, bdmInstance.BdmSubEntry.Effect3_SkillID, bdmInstance.BdmSubEntry.Effect3_ID, bdmInstance);
        }

        public async void PlayEffect(BSA_Type6 bsaEffect, Move move, Actor actor, Matrix4x4 world)
        {
            if (!TryReserveEffectSlot())
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            try
            {
                int generation = effectGeneration;

                EffectContainerFile eepk = Files.Instance.GetEepkFile((BAC_Type8.EepkTypeEnum)bsaEffect.EepkType, bsaEffect.SkillID, move, actor, true);

                if (eepk != null)
                {
                    Effect eepkEffect = eepk.GetEffect(bsaEffect.EffectID);

                    if (eepkEffect != null)
                    {
                        Matrix4x4 spawnPosition = world * Matrix4x4.CreateTranslation(new SimdVector3(bsaEffect.F_12, bsaEffect.F_16, bsaEffect.F_20));
                        await Task.Run(() => AddEffect(actor, eepkEffect, spawnPosition, generation));
                    }
                    else
                    {
                        Log.Add($"No effect at ID {bsaEffect.EffectID} could be found in EEPK {bsaEffect.EepkType}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add($"VfxManager.PlayEffect: could not play effect. {ex.Message}", LogType.Warning);
            }
            finally
            {
                ReleaseEffectSlot();
            }
        }

        public VfxEffect PlayProjectileEffect(BSA_Type6 bsaEffect, Move move, Actor actor, Matrix4x4 world)
        {
            if (!TryReserveEffectSlot())
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return null;
            }

            try
            {
                EffectContainerFile eepk = Files.Instance.GetEepkFile((BAC_Type8.EepkTypeEnum)bsaEffect.EepkType, bsaEffect.SkillID, move, actor, true);

                if (eepk != null)
                {
                    Effect eepkEffect = eepk.GetEffect(bsaEffect.EffectID);

                    if (eepkEffect != null)
                    {
                        return AddEffect(actor, eepkEffect, world, effectGeneration, true);
                    }

                    Log.Add($"No effect at ID {bsaEffect.EffectID} could be found in EEPK {bsaEffect.EepkType}.");
                }

                return null;
            }
            finally
            {
                ReleaseEffectSlot();
            }
        }

        private async void PlayEffect(BAC_Type8.EepkTypeEnum eepkType, ushort skillId, short effectId, DamageManager bdmInstance)
        {
            if (!TryReserveEffectSlot())
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            if (effectId == -1)
            {
                ReleaseEffectSlot();
                return;
            }

            try
            {
                int generation = effectGeneration;

                EffectContainerFile eepk = Files.Instance.GetEepkFile(eepkType, skillId, bdmInstance.Move, bdmInstance.Victim, true);

                if (eepk != null)
                {
                    Effect eepkEffect = eepk.GetEffect(effectId);

                    if (eepkEffect != null)
                    {
                        await Task.Run(() => AddEffect(bdmInstance.Victim, eepkEffect, bdmInstance.HitPosition, generation));
                    }
                    else
                    {
                        Log.Add($"No effect at ID {effectId} could be found in EEPK {eepkType}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add($"VfxManager.PlayEffect: could not play effect. {ex.Message}", LogType.Warning);
            }
            finally
            {
                ReleaseEffectSlot();
            }
        }

        private bool TryReserveEffectSlot()
        {
            lock (Effects)
            {
                if (Effects.Count + NewEffects.Count + pendingEffectLoads >= MAX_EFFECTS)
                {
                    return false;
                }

                pendingEffectLoads++;
                return true;
            }
        }

        private void ReleaseEffectSlot()
        {
            Interlocked.Decrement(ref pendingEffectLoads);
        }

        private VfxEffect AddEffect(Actor actor, Effect effect, Matrix4x4 world, int generation, bool spawnedByProjectile = false)
        {
            VfxEffect vfxEffect = new VfxEffect(actor, effect, world, spawnedByProjectile);

            lock (Effects)
            {
                if (generation != effectGeneration)
                {
                    vfxEffect.Dispose();
                    return null;
                }

                NewEffects.Add(vfxEffect);
            }

            return vfxEffect;
        }

        public void StopActorEffects(Actor actor)
        {
            lock (Effects)
            {
                foreach (VfxEffect vfxEffect in Effects)
                {
                    if (vfxEffect.Actor == actor)
                        vfxEffect.Terminate(true);
                }
            }
        }

        public void StopEffect(BAC_Type8 bacEffect, BacEntryInstance bacInstance)
        {
            EffectContainerFile eepk = Files.Instance.GetEepkFile(bacEffect.EepkType, bacEffect.SkillID, bacInstance.SkillMove, bacInstance.User, true);

            if (eepk != null)
            {
                Effect eepkEffect = eepk.GetEffect(bacEffect.EffectID);

                if (eepkEffect != null)
                {
                    StopEffect(eepkEffect);
                }
            }
        }

        public void StopEffect(Effect effect)
        {
            lock (Effects)
            {
                foreach (VfxEffect vfxEffect in Effects)
                {
                    if (vfxEffect.Effect == effect)
                        vfxEffect.Terminate(false);
                }

                foreach (VfxEffect vfxEffect in NewEffects)
                {
                    if (vfxEffect.Effect == effect)
                        vfxEffect.Terminate(false);
                }
            }
        }

        public void StopEffects()
        {
            Interlocked.Increment(ref effectGeneration);

            lock (Effects)
            {
                foreach (VfxEffect effect in Effects)
                {
                    effect.Dispose();
                }

                NewEffects.Clear();
                Effects.Clear();
            }

        }

        public void RestartEffect()
        {
            if(Effects.Count == 1)
            {
                Effects[0].Initialize();
            }
        }
        #endregion

        #region UpdateAndRendering
        public override void Update()
        {
            Update(false);
        }

        private void Update(bool simulate)
        {
            lock (Effects)
            {
                if (NewEffects.Count > 0)
                {
                    Effects.AddRange(NewEffects);
                    NewEffects.Clear();
                }

                for (int i = Effects.Count - 1; i >= 0; i--)
                {
                    if (Effects[i].IsDestroyed)
                    {
                        Effects[i].Dispose();
                        Effects.RemoveAt(i);
                        continue;
                    }

                    if (simulate)
                    {
                        Effects[i].Simulate();
                    }
                    else
                    {
                        Effects[i].Update();
                    }
                }
            }

            ForceEffectUpdate = false;
        }

        public void Simulate()
        {
            Update(true);
        }

        public override void Draw()
        {
            /*
            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation) return;

            
            foreach(VfxEffect effect in Effects)
            {
                effect.Draw();
            }
            */
        }

        #endregion

        /// <summary>
        /// Returns the first active <see cref="VfxColorFade"/> matching the conditions.
        /// </summary>
        public VfxColorFadeEntry GetActiveColorFade(string materialName, Actor actor)
        {
            if(SceneManager.IsOnEffectTab)
            {
                return Viewport.Instance.VfxPreview.GetActiveColorFade(materialName, actor);
            }

            foreach(VfxEffect effect in Effects.Where(x => x.Actor == actor))
            {
                foreach(VfxAsset asset in effect.Assets)
                {
                    if(asset is VfxColorFade colorFade)
                    {
                        VfxColorFadeEntry entry = colorFade.GetColorFadeEntry(materialName);

                        if(entry != null)
                            return entry;
                    }
                }
            }

            return null;
        }

        public VfxLight GetActiveLight(Matrix world)
        {
            if (SceneManager.IsOnEffectTab)
            {
                return Viewport.Instance.VfxPreview.GetActiveLight();
            }

            foreach (VfxEffect effect in Effects)
            {
                foreach (VfxAsset asset in effect.Assets)
                {
                    if (asset is VfxLight light)
                    {
                        return light;
                    }
                }
            }

            return null;
        }
    }
}
