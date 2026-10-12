using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Character;
using XenoKit.Engine.Vfx.Asset;
using Xv2CoreLib.ACB;
using Xv2CoreLib.AUR;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BPE;
using Xv2CoreLib.Resource.App;
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

        /// <summary>
        /// Force effects to fully update on the next cycle, even if its via Simulate().
        /// </summary>
        public bool ForceEffectUpdate { get; set; }


        #region PlayAndStop
        public async void PlayEffect(Effect effect, Actor actor, bool hideWithActor = false)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            await Task.Run(() => AddEffect(actor, effect, Matrix4x4.Identity, hideWithActor: hideWithActor));
        }

        public async void PlayEffect(BAC_Type8 bacEffect, BacEntryInstance bacInstance, Actor actor)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            int skillId = bacEffect.UseSkillId == BAC_Type8.UseSkillIdEnum.True ? bacEffect.SkillID : 0;

            EffectContainerFile eepk = Files.Instance.GetEepkFile(bacEffect.EepkType, (ushort)skillId, bacInstance.SkillMove, bacInstance.User, true);

            if (eepk != null)
            {
                Effect eepkEffect = eepk.GetEffect(bacEffect.EffectID);

                if (eepkEffect != null)
                {
                    //Get spawn position from declared bone and position on the bac entry
                    Matrix4x4 spawnPosition = Matrix4x4.Identity;

                    if(actor != null && (int)bacEffect.BoneLink < 25)
                    {
                        spawnPosition = actor.GetAbsoluteBoneMatrix(actor.Skeleton.BAC_BoneIndices[(int)bacEffect.BoneLink]) * Matrix4x4.CreateTranslation(new SimdVector3(bacEffect.PositionX, bacEffect.PositionY, bacEffect.PositionZ));
                    }

                    await Task.Run(() => AddEffect(bacInstance.User, eepkEffect, spawnPosition));
                }
                else
                {
                    Log.Add($"No effect at ID {bacEffect.EffectID} could be found in EEPK {bacEffect.EepkType}.");
                }
            }
        }

        public void PlayEffect(DamageManager bdmInstance)
        {
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect1_EepkType, bdmInstance.BdmSubEntry.Effect1_SkillID, bdmInstance.BdmSubEntry.Effect1_ID, bdmInstance);
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect2_EepkType, bdmInstance.BdmSubEntry.Effect2_SkillID, bdmInstance.BdmSubEntry.Effect2_ID, bdmInstance);
            PlayEffect((BAC_Type8.EepkTypeEnum)bdmInstance.BdmSubEntry.Effect3_EepkType, bdmInstance.BdmSubEntry.Effect3_SkillID, bdmInstance.BdmSubEntry.Effect3_ID, bdmInstance);
        }

        public VfxEffect PlayProjectileEffect(BSA_Type6 bsaEffect, Move move, Actor actor, Matrix4x4 world, Matrix4x4 nodeTransform)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return null;
            }

            EffectContainerFile eepk = Files.Instance.GetEepkFile((BAC_Type8.EepkTypeEnum)bsaEffect.EepkType, bsaEffect.SkillID, move, actor, true);

            if (eepk != null)
            {
                Effect eepkEffect = eepk.GetEffect(bsaEffect.EffectID);

                if (eepkEffect != null)
                {
                    return AddEffect(actor, eepkEffect, world, true, nodeTransform);
                }

                Log.Add($"No effect at ID {bsaEffect.EffectID} could be found in EEPK {bsaEffect.EepkType}.");
            }

            return null;
        }

        private async void PlayEffect(BAC_Type8.EepkTypeEnum eepkType, ushort skillId, short effectId, DamageManager bdmInstance)
        {
            if (Effects.Count >= MAX_EFFECTS)
            {
                Log.Add("Maximum amount of effects that can be active at the same time reached. Cannot start new ones.", LogType.Warning);
                return;
            }

            if (effectId == -1) return;

            EffectContainerFile eepk = Files.Instance.GetEepkFile(eepkType, skillId, bdmInstance.Move, bdmInstance.Victim, true);

            if (eepk != null)
            {
                Effect eepkEffect = eepk.GetEffect(effectId);

                if (eepkEffect != null)
                {
                    await Task.Run(() => AddEffect(bdmInstance.Victim, eepkEffect, bdmInstance.HitPosition));
                }
                else
                {
                    Log.Add($"No effect at ID {effectId} could be found in EEPK {eepkType}.");
                }
            }
        }

        private VfxEffect AddEffect(Actor actor, Effect effect, Matrix4x4 world, bool spawnedByProjectile = false, Matrix4x4? nodeTransform = null, bool hideWithActor = false)
        {
            VfxEffect vfxEffect = new VfxEffect(actor, effect, world, spawnedByProjectile, nodeTransform);
            vfxEffect.HideWithActor = hideWithActor;

            lock (Effects)
            {
                NewEffects.Add(vfxEffect);
            }

            return vfxEffect;
        }

        /// <summary>
        /// Finds a playing asset for the given effect part, so the editor can show a gizmo where the effect actually is.
        /// </summary>
        public VfxAsset FindPlayingAsset(EffectPart effectPart)
        {
            if (effectPart == null) return null;

            lock (Effects)
            {
                foreach (VfxEffect effect in Effects)
                {
                    VfxAsset asset = effect.FindAsset(effectPart);

                    if (asset != null) return asset;
                }
            }

            return null;
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

        //BTL_AURA.eepk in the common EEPK list
        private const ushort AuraEepkId = 1;
        //Cues in the common SE bank: the boost loop sound and the boost end sound
        private const int AuraLoopCue = 100;
        private const int AuraEndCue = 101;

        /// <summary>
        /// Plays or stops the effect for the row's aura type. The EEPK decides the bone and whether it loops, and nothing ends it when the row ends.
        /// </summary>
        public void PlayAura(BAC_Type19 row, Actor actor)
        {
            PlayAura(row.AuraType, row.AuraFlags.HasFlag(BAC_Type19.AuraFlagsEnum.DisableAura), actor);
        }

        private void PlayAura(AuraType type, bool stop, Actor actor)
        {
            AUR_Aura aura = Files.Instance.GetAura(actor, out int auraId, out _);
            EffectContainerFile eepk = Files.Instance.GetEepkFile(BAC_Type8.EepkTypeEnum.Common, AuraEepkId, null, actor, true);

            if (aura == null || eepk == null) return;

            List<AUR_Effect> auraEffects = aura.AuraEffects;
            PlayAuraSound(type, stop, actor);

            //Any aura call restarts the outline, and a stop removes it. Auras without an outline also remove it.
            BPE_Entry outline = stop ? null : Files.Instance.GetAuraOutline(auraId);

            if (outline != null)
                actor.StartAuraOutline(outline);
            else
                actor.StopAuraOutline();

            if (!stop && (type == AuraType.BoostStart || type == AuraType.BoostLoop))
            {
                StopEffect(GetAuraEffect(eepk, auraEffects, AuraType.KiaiCharge));
                StopEffect(GetAuraEffect(eepk, auraEffects, AuraType.HenshinStart));
            }

            Effect effect = GetAuraEffect(eepk, auraEffects, type);

            if (stop)
            {
                StopEffect(effect);

                //When an aura stops, the Glare aura starts again, as the game re-adds HenshinStart
                StartPermanentAura(actor);
            }
            else if (effect != null)
            {
                PlayEffect(effect, actor, true);
            }
        }

        //Characters with Glare set in aura_setting.aur start their HenshinStart aura when they spawn
        private void StartPermanentAura(Actor actor)
        {
            Files.Instance.GetAura(actor, out _, out bool glare);

            if (!glare || !SettingsManager.settings.XenoKit_VfxSimulation || !SettingsManager.settings.XenoKit_CharacterAuraSimulation) return;

            PlayAura(AuraType.HenshinStart, false, actor);
        }

        private void PlayAuraSound(AuraType type, bool stop, Actor actor)
        {
            ACB_Wrapper acb = Files.Instance.GetAcbFile(Xv2CoreLib.BAC.AcbType.Common_SE, null, actor, false);

            if (acb == null || !ViewportInstance.IsPlaying) return;

            if (type == AuraType.BoostEnd || (stop && type == AuraType.BoostLoop))
                ViewportInstance.AudioEngine.StopCue(AuraLoopCue, acb.AcbFile.Name);

            if (stop) return;

            if (type == AuraType.BoostStart)
                ViewportInstance.AudioEngine.PlayCue(AuraLoopCue, acb, actor);
            else if (type == AuraType.BoostEnd)
                ViewportInstance.AudioEngine.PlayCue(AuraEndCue, acb, actor);
        }

        private static Effect GetAuraEffect(EffectContainerFile eepk, List<AUR_Effect> auraEffects, AuraType type)
        {
            int index = (int)type;

            if (index >= auraEffects.Count || auraEffects[index].I_04 < 0) return null;

            Effect effect = eepk.GetEffect(auraEffects[index].I_04);

            if (effect == null)
                Log.Add($"VfxManager.PlayAura: no effect {auraEffects[index].I_04} in BTL_AURA.eepk for aura type {type}.", LogType.Warning);

            return effect;
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
            lock (Effects)
            {
                foreach (VfxEffect effect in Effects)
                {
                    effect.Dispose();
                }

                NewEffects.Clear();
                Effects.Clear();
            }

            foreach (Actor actor in SceneManager.Actors)
            {
                if (actor == null) continue;

                actor.StopAuraOutline();
                StartPermanentAura(actor);
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
                Effects.AddRange(NewEffects);
                NewEffects.Clear();


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
