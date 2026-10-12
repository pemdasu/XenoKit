using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.AUR;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BPE;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ERS;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Eternity;
using xv2 = Xv2CoreLib.Xenoverse2;
using @file = Xv2CoreLib.FileManager;
using XenoKit.Engine;

namespace XenoKit.Editor
{
    public sealed partial class Files : INotifyPropertyChanged
    {
        public Move GetCmnMove()
        {
            var move = OutlinerItems.FirstOrDefault(x => x.Type == OutlinerItem.OutlinerItemType.CMN);
            return move != null ? move.move : null;
        }

        public BPE_File CommonBpeFile => CmnBpeFile;

        //The game's hard-coded aura id to BPE body outline table. XV2Patcher's pre-baked.xml AuraExtraData replaces entries.
        private static readonly Dictionary<int, ushort> AuraOutlineBpeIds = new Dictionary<int, ushort>()
        {
            { 0x1A, 0x53 }, { 0x1B, 0x53 }, { 0x1D, 0x53 }, { 0x1F, 0x53 }, { 0x32, 0x53 },
            { 0x24, 0x101 }, { 0x27, 0x104 }, { 0x2D, 0x109 }, { 0x2E, 0x111 }, { 0x33, 0x118 }, { 0x34, 0x119 }, { 0x35, 0x12E }, { 0x3D, 0x154 },
            { 0x3F, 0x172 }, { 0x43, 0x172 }, { 0x44, 0x172 }, { 0x46, 0x172 }, { 0x40, 0x173 },
            { 0x39, 0x140 }, { 0x3A, 0x140 }, { 0x3B, 0x140 }, { 0x3C, 0x140 }, { 0x41, 0x140 }, { 0x42, 0x140 }, { 0x45, 0x140 }, { 0x47, 0x140 }
        };

        /// <summary>
        /// The actor's aura from aura_setting.aur. The game falls back to costume 0, and to aura 0 when the character has no entry.
        /// </summary>
        /// <param name="glare">The Glare value of the character entry. The character starts this aura when it spawns.</param>
        public AUR_Aura GetAura(Actor actor, out int auraId, out bool glare)
        {
            auraId = 0;
            glare = false;

            if (CmnAuraFile == null || actor?.CharacterData?.CmsEntry == null) return null;

            int charaId = actor.CharacterData.CmsEntry.ID;
            AUR_Character chara = CmnAuraFile.CharacterAuras.FirstOrDefault(x => x.CharaID == charaId && x.Costume == actor.PartSet.ID)
                ?? CmnAuraFile.CharacterAuras.FirstOrDefault(x => x.CharaID == charaId && x.Costume == 0);

            glare = chara?.I_12 == true;
            auraId = chara != null && chara.AuraID > 0 ? chara.AuraID : 0;

            if (auraId >= CmnAuraFile.Auras.Count)
            {
                Log.Add($"Files.GetAura: aura {auraId} for character {charaId} is not in aura_setting.aur.", LogType.Warning);
                return null;
            }

            return CmnAuraFile.Auras[auraId];
        }

        /// <summary>
        /// The BPE body outline that shows while the aura is on, or null when the aura has none.
        /// </summary>
        public BPE_Entry GetAuraOutline(int auraId)
        {
            PreBakedAuraExtraData extra = CmnPrebakedFile?.AuraExtras?.FirstOrDefault(x => x.AuraID == auraId);
            ushort bpeId;

            if (extra != null)
                bpeId = extra.BPE_ID;
            else if (!AuraOutlineBpeIds.TryGetValue(auraId, out bpeId))
                return null;

            return GetBpeEntry(bpeId, true);
        }

        public BPE_Entry GetBpeEntry(ushort bpeIndex, bool logErrors)
        {
            BPE_Entry entry = CmnBpeFile?.Entries?.FirstOrDefault(x => x.SortID == bpeIndex);

            if (entry == null && logErrors)
                Log.Add($"Files.GetBpeEntry: Could not find BPE entry {bpeIndex}.", LogType.Warning);

            return entry;
        }

        public List<Actor> GetLoadedCharacters()
        {
            List<Actor> chars = new List<Actor>();

            foreach (OutlinerItem item in OutlinerItems.Where(x => x.Type == OutlinerItem.OutlinerItemType.Character || x.Type == OutlinerItem.OutlinerItemType.CaC))
                chars.Add(item.character);

            return chars;
        }

        public object TryGetFileInstance(string path)
        {
            foreach (var outlinerItem in OutlinerItems)
            {
                if (outlinerItem.move != null)
                {
                    var ret = outlinerItem.move.TryGetFileInstance(path);
                    if (ret != null) return ret;
                }
                else if (outlinerItem.character?.Moveset != null)
                {
                    var ret = outlinerItem.character?.Moveset.TryGetFileInstance(path);
                    if (ret != null) return ret;
                }
            }

            return null;
        }

        public EAN_File GetEanFile(BAC_Type0.EanTypeEnum eanType, Move move, Actor character, bool logErrors, bool charaUnique)
        {
            switch (eanType)
            {
                case BAC_Type0.EanTypeEnum.Common:
                case BAC_Type0.EanTypeEnum.MCM_DBA:
                case BAC_Type0.EanTypeEnum.MCM_TTL:
                case BAC_Type0.EanTypeEnum.MCM_TU6:
                case BAC_Type0.EanTypeEnum.MCM_TU13_5:
                case BAC_Type0.EanTypeEnum.MCM_LeftHand:
                case BAC_Type0.EanTypeEnum.MCM_BothHands:
                case BAC_Type0.EanTypeEnum.MCM_XV1:
                case BAC_Type0.EanTypeEnum.MCM_CommonFull:
                case BAC_Type0.EanTypeEnum.CM2_XAC:
                case BAC_Type0.EanTypeEnum.CM2_XAN:
                    return Instance.GetCmnMove().Files.EanFile.FirstOrDefault(x => x.Costumes.Contains((int)eanType))?.File;
                case BAC_Type0.EanTypeEnum.CommonTTL:
                case BAC_Type0.EanTypeEnum.CM2_XVS:
                    if (character == null) return null;
                    var bcs = character.CharacterData.BcsFile.File;
                    string race = System.IO.Path.GetFileNameWithoutExtension(Xv2CoreLib.BCS.BCS_File.GetBcsFilePath(bcs.Race, bcs.Gender));
                    if (eanType == BAC_Type0.EanTypeEnum.CommonTTL && bcs.Race == Xv2CoreLib.BCS.Race.Saiyan)
                        race = bcs.Gender == Xv2CoreLib.BCS.Gender.Male ? "SYM" : "SYF";
                    return Instance.GetCmnMove().Files.EanFile.FirstOrDefault(x => x.Costumes.Contains((int)eanType) && x.CharaCode == race)?.File;
                case BAC_Type0.EanTypeEnum.Character:
                    if (move == null) return character?.Moveset.Files.GetEanFile(character.ShortName, charaUnique);
                    return (move.MoveType == Move.Type.Moveset) ? move.Files.GetEanFile(character?.ShortName, charaUnique) : character?.Moveset.Files.GetEanFile(character.ShortName, charaUnique);
                case BAC_Type0.EanTypeEnum.Skill:
                    if (move == null)
                    {
                        if (logErrors) Log.Add("Files.GetEanFile: Cannot use skill EAN file from this bac. No attached EAN was found.", LogType.Warning);
                        return null;
                    }
                    if (move.MoveType == Move.Type.Moveset)
                    {
                        if (logErrors) Log.Add("Files.GetEanFile: Cannot use skill EAN file from a moveset.", LogType.Warning);
                        return null;
                    }
                    return move.Files.GetEanFile(character?.ShortName, charaUnique);
                case BAC_Type0.EanTypeEnum.FaceBase:
                    {
                        if (character == null) return null;
                        var faceEan = character.CharacterData.IsCaC ? character.FceEanFile : character.FceEanFile;

                        if (faceEan == null && logErrors)
                        {
                            Log.Add($"Face Ean file was requested for {character.Name}, but none was loaded with the character!", LogType.Warning);
                        }

                        return faceEan;
                    }
                case BAC_Type0.EanTypeEnum.FaceForehead:
                    {
                        if (character == null) return null;
                        var faceEan = character.CharacterData.IsCaC ? character.FceEyeEanFile : character.FceEanFile;

                        if (faceEan == null && logErrors)
                        {
                            Log.Add($"Face Ean file was requested for {character.Name}, but none was loaded with the character!", LogType.Warning);
                        }

                        return faceEan;
                    }
                case BAC_Type0.EanTypeEnum.CommonTail:
                    return Files.Instance.GetCmnMove().Files.GetTailEanFile();
            }

            if (logErrors) Log.Add($"Files.GetEanFile: Could not find the ean file for \"{eanType}\".", LogType.Warning);
            return null;
        }

        public EAN_File GetCamEanFile(BAC_Type10.EanTypeEnum eanType, Move move, Actor character, bool logErrors, bool charaUnique)
        {
            //Dont log errors for unknown/unimplemented cam.ean files (too much needless log spam)
            if (eanType != BAC_Type10.EanTypeEnum.Character && eanType != BAC_Type10.EanTypeEnum.Common && eanType != BAC_Type10.EanTypeEnum.Skill)
                logErrors = false;

            switch (eanType)
            {
                case BAC_Type10.EanTypeEnum.Common:
                case BAC_Type10.EanTypeEnum.MCM:
                case BAC_Type10.EanTypeEnum.CommonTTL:
                    return Instance.GetCmnMove().Files.CamEanFile.FirstOrDefault(x => x.Costumes.Contains((int)eanType))?.File;
                case BAC_Type10.EanTypeEnum.Character:
                    if (move == null) return character.Moveset.Files.GetCamEanFile(character.ShortName, charaUnique);
                    return (move.MoveType == Move.Type.Moveset) ? move.Files.GetCamEanFile(character.ShortName, charaUnique) : character.Moveset.Files.GetCamEanFile(character.ShortName, charaUnique);
                case BAC_Type10.EanTypeEnum.Skill:
                    if (move == null)
                    {
                        if (logErrors) Log.Add("Files.GetCamFile: Cannot use skill EAN file from this bac. No attached ean was found.", LogType.Warning);
                        return null;
                    }
                    if (move.MoveType == Move.Type.Moveset)
                    {
                        if (logErrors) Log.Add("Files.GetCamFile: Cannot use skill EAN file from a moveset.", LogType.Warning);
                        return null;
                    }
                    return move.Files.GetCamEanFile(character?.ShortName, charaUnique);
            }

            if (logErrors) Log.Add($"Files.GetCamFile: Could not find the ean file for \"{eanType}\".", LogType.Warning);
            return null;
        }

        public BDM_File GetBdmFile(BAC_Type1.BdmType bdmType, Move move, Actor character, bool logErrors)
        {
            switch (bdmType)
            {
                case BAC_Type1.BdmType.Common:
                    return Instance.GetCmnMove().Files.BdmFile.File;
                case BAC_Type1.BdmType.Character:
                    if (move.MoveType == Move.Type.Skill || move.MoveType == Move.Type.CMN)
                    {
                        if (character == null && logErrors)
                            Log.Add("Files.GetBdmFile: BDM Type is Character but no character was passed in as a parameter, cannot return BDM file.", LogType.Warning);
                        return character?.Moveset.Files.BdmFile.File;
                    }
                    return move?.Files.BdmFile.File;
                case BAC_Type1.BdmType.Skill:
                    if (move.MoveType == Move.Type.Moveset)
                    {
                        if (logErrors) Log.Add("Files.GetBdmFile: Cannot use skill BDM file from a moveset.", LogType.Warning);
                        return null;
                    }
                    return move?.Files.BdmFile.File;
                default:
                    return null;

            }
        }

        public EffectContainerFile GetEepkFile(BAC_Type8.EepkTypeEnum eepkType, ushort skillId, Move move, Actor character, bool logErrors)
        {
            switch (eepkType)
            {
                case BAC_Type8.EepkTypeEnum.StageBG:
                case BAC_Type8.EepkTypeEnum.Stage:
                    return GetStageEepkFile(eepkType, logErrors);
                case BAC_Type8.EepkTypeEnum.AwokenSkill: //AnySkill
                case BAC_Type8.EepkTypeEnum.SuperSkill:
                case BAC_Type8.EepkTypeEnum.UltimateSkill:
                case BAC_Type8.EepkTypeEnum.EvasiveSkill:
                case BAC_Type8.EepkTypeEnum.KiBlastSkill:
                case BAC_Type8.EepkTypeEnum.NEW_AwokenSkill: //The real awoken skill type
                    return (skillId == move.SkillID && move.MoveType == Move.Type.Skill) ? move.Files.EepkFile.File : null;
                case BAC_Type8.EepkTypeEnum.Character:
                    if (move.MoveType == Move.Type.Moveset) return move.Files.EepkFile.File;
                    if (character == null && logErrors)
                        Log.Add("Files.GetEepkFile: EEPK Type is Character but no character was passed in as a parameter, cannot return EEPK file.", LogType.Warning);
                    return character.Moveset.Files.EepkFile.File;
                case BAC_Type8.EepkTypeEnum.Common:
                    Xv2File<EffectContainerFile> eepk = GetCmnMove().Files.EepkFiles.FirstOrDefault(x => x.Costumes.Contains(skillId));
                    return eepk?.File;
            }
            return null;
        }

        public EffectContainerFile GetStageEepkFile(BAC_Type8.EepkTypeEnum eepkType, bool logErrors)
        {
            if (!TryGetStageErsTableId(eepkType, out int tableId))
            {
                if (logErrors)
                    Log.Add($"Files.GetStageEepkFile: EEPK type \"{eepkType}\" is not a stage EEPK type.", LogType.Warning);
                return null;
            }

            var stage = Viewport.Instance?.CurrentStage?.StageDefEntry;

            if (stage == null)
            {
                if (eepkType == BAC_Type8.EepkTypeEnum.StageBG)
                    return GetCachedStageEepk("vfx/bg/LND/BG_LND.eepk", logErrors);

                if (logErrors)
                    Log.Add($"Files.GetStageEepkFile: No current stage is loaded. EEPK Type = {eepkType}, ERS Table = {tableId}.", LogType.Warning);
                return null;
            }

            if (string.IsNullOrWhiteSpace(stage.CODE))
            {
                if (logErrors)
                    Log.Add($"Files.GetStageEepkFile: Current stage has no stage code. Stage Index = {stage.Index}, EEPK Type = {eepkType}, ERS Table = {tableId}.", LogType.Warning);
                return null;
            }

            ERS_File ersFile = xv2.Instance.ErsFile;

            if (ersFile == null)
            {
                if (logErrors)
                    Log.Add($"Files.GetStageEepkFile: vfx_spec.ers is not loaded. Stage = {stage.CODE}, Stage Index = {stage.Index}, EEPK Type = {eepkType}, ERS Table = {tableId}.", LogType.Warning);
                return null;
            }

            List<ERS_MainTableEntry> subEntries = ersFile.GetSubentryList(tableId);

            if (subEntries == null)
            {
                if (logErrors)
                    Log.Add($"Files.GetStageEepkFile: vfx_spec.ers table {tableId} was not found. Stage = {stage.CODE}, Stage Index = {stage.Index}, EEPK Type = {eepkType}.", LogType.Warning);
                return null;
            }

            ERS_MainTableEntry entry = subEntries.FirstOrDefault(x => string.Equals(x.Str_04, stage.CODE, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                if (logErrors)
                    Log.Add($"Files.GetStageEepkFile: No vfx_spec.ers entry was found for stage \"{stage.CODE}\". Stage Index = {stage.Index}, EEPK Type = {eepkType}, ERS Table = {tableId}.", LogType.Warning);
                return null;
            }

            if (string.IsNullOrWhiteSpace(entry.FILE_PATH))
            {
                if (logErrors)
                    Log.Add($"Files.GetStageEepkFile: vfx_spec.ers entry for stage \"{stage.CODE}\" has no file path. Stage Index = {stage.Index}, EEPK Type = {eepkType}, ERS Table = {tableId}, ERS Entry = {entry.ID}.", LogType.Warning);
                return null;
            }

            string path = GetStageEepkPath(entry.FILE_PATH);
            return GetCachedStageEepk(path, logErrors);
        }

        private static bool TryGetStageErsTableId(BAC_Type8.EepkTypeEnum eepkType, out int tableId)
        {
            switch (eepkType)
            {
                case BAC_Type8.EepkTypeEnum.StageBG:
                    tableId = 1;
                    return true;
                case BAC_Type8.EepkTypeEnum.Stage:
                    tableId = 11;
                    return true;
                default:
                    tableId = -1;
                    return false;
            }
        }

        private static string GetStageEepkPath(string ersFilePath)
        {
            return $"vfx/{ersFilePath.Replace('\\', '/').TrimStart('/')}";
        }

        private EffectContainerFile GetCachedStageEepk(string path, bool logErrors)
        {
            if (stageEepkCache.TryGetValue(path, out EffectContainerFile cachedFile))
                return cachedFile;

            try
            {
                EffectContainerFile eepk = file.Instance.LoadFile<EffectContainerFile>(path, false, false);
                if (eepk != null)
                {
                    stageEepkCache[path] = eepk;
                    return eepk;
                }
            }
            catch
            {
            }

            if (logErrors && missingStageEepks.Add(path))
                Log.Add($"Files.GetStageEepkFile: Could not find stage EEPK \"{path}\".", LogType.Warning);

            return null;
        }

        public BSA_File GetBsaFile(BAC_Type9.BsaTypeEnum bsaType, ushort skillId, Move move, Actor character, bool logErrors)
        {
            switch (bsaType)
            {
                case BAC_Type9.BsaTypeEnum.Common:
                    return GetCmnMove().Files.BsaFile.File;
                case BAC_Type9.BsaTypeEnum.AwokenSkill:
                case BAC_Type9.BsaTypeEnum.EvasiveSkill:
                case BAC_Type9.BsaTypeEnum.KiBlastSkill:
                case BAC_Type9.BsaTypeEnum.SuperSkill:
                case BAC_Type9.BsaTypeEnum.UltimateSkill:
                case BAC_Type9.BsaTypeEnum.NEW_AwokenSkill:
                    if (move.SkillID == skillId && move.MoveType == Move.Type.Skill) return move.Files.BsaFile.File;
                    return null;
            }
            return null;
        }

        public ACB_Wrapper GetAcbFile(Xv2CoreLib.BAC.AcbType acbType, Move move, Actor character, bool logErrors)
        {
            switch (acbType)
            {
                case Xv2CoreLib.BAC.AcbType.Common_SE:
                    return GetCmnMove().Files.GetSeFile();
                case Xv2CoreLib.BAC.AcbType.Footsteps:
                    return CmnFootstepsAcbFile;
                case Xv2CoreLib.BAC.AcbType.Mission_SE:
                    return CmnMissionAcbFile;
                case Xv2CoreLib.BAC.AcbType.Character_SE:
                    return (move.MoveType == Move.Type.Skill || move.MoveType == Move.Type.CMN) ? character?.Moveset.Files.GetSeFile() : move.Files.GetSeFile();
                case Xv2CoreLib.BAC.AcbType.Skill_SE:
                    return move.Files.GetSeFile();
                case Xv2CoreLib.BAC.AcbType.Skill_VOX:
                    if (character == null && logErrors)
                        Log.Add("Files.GetAcbFile: Cannot get Skill_VOX ACB as no character is present!", LogType.Error);

                    return move?.Files.GetVoxFile(character.SkillVoiceAlias != null ? character.SkillVoiceAlias : character.ShortName, 0, true);
                case Xv2CoreLib.BAC.AcbType.Character_VOX:
                    if (character == null && logErrors)
                        Log.Add("Files.GetAcbFile: Cannot get Character_VOX ACB as no character is present!", LogType.Error);
                    return character?.Moveset.Files.GetVoxFile(character.Voice, true);

            }
            return null;
        }

    }
}
