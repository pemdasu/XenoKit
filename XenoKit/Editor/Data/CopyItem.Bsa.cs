using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Editor
{
    public partial class CopyItem
    {
        private void CopyBsaEntryReferences(BSA_Entry bsaEntry, Move move)
        {
            //Passing
            if(CopySelfProjectile(bsaEntry.Expires, move))
                ValueRefs.Add(new ValueReference(bsaEntry, nameof(bsaEntry.Expires), ValueReference.InstanceRefType.Bsa));

            if (CopySelfProjectile(bsaEntry.ImpactEnemy, move))
                ValueRefs.Add(new ValueReference(bsaEntry, nameof(bsaEntry.ImpactEnemy), ValueReference.InstanceRefType.Bsa));

            if (CopySelfProjectile(bsaEntry.ImpactGround, move))
                ValueRefs.Add(new ValueReference(bsaEntry, nameof(bsaEntry.ImpactGround), ValueReference.InstanceRefType.Bsa));

            if (CopySelfProjectile(bsaEntry.ImpactProjectile, move))
                ValueRefs.Add(new ValueReference(bsaEntry, nameof(bsaEntry.ImpactProjectile), ValueReference.InstanceRefType.Bsa));

            //Collision
            if (bsaEntry.SubEntries?.CollisionEntries != null)
            {
                foreach (var unk1 in bsaEntry.SubEntries.CollisionEntries)
                {
                    if (CopyEffect((BAC_Type8.EepkTypeEnum)unk1.EepkType, unk1.EffectID, unk1.SkillID, move))
                    {
                        ValueRefs.Add(new ValueReference(unk1, nameof(unk1.EffectID), ValueReference.InstanceRefType.Eepk));
                        ValueRefs.Add(new ValueReference(unk1, nameof(unk1.EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                        ValueRefs.Add(new ValueReference(unk1, nameof(unk1.SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
                    }
                }
            }

            //Collision Sound
            if (bsaEntry.SubEntries?.ExpirationEntries != null)
            {
                foreach (var expiration in bsaEntry.SubEntries.ExpirationEntries)
                {
                    if (BsaSoundResources.TryGetBacAcbType(expiration.I_00, out Xv2CoreLib.BAC.AcbType acbType) &&
                        CopyCue(acbType, expiration.I_04, move))
                    {
                        ValueRefs.Add(new ValueReference(expiration, nameof(expiration.I_04), ValueReference.InstanceRefType.SeAcb));
                        ValueRefs.Add(new ValueReference(expiration, nameof(expiration.I_00), ValueReference.InstanceRefType.SeAcb, ValueReference.Mode.Type));
                    }
                }
            }

            //Types
            if (bsaEntry.IBsaTypes == null) return;

            foreach(var bsaType in bsaEntry.IBsaTypes)
            {
                switch (bsaType)
                {
                    case BSA_Type0 type0:
                        CopyBsaType0References(type0, move);
                        break;
                    case BSA_Type3 type3:
                        CopyBsaType3References(type3, move);
                        break;
                    case BSA_Type6 type6:
                        CopyBsaType6References(type6, move);
                        break;
                    case BSA_Type7 type7:
                        CopyBsaType7References(type7, move);
                        break;
                    case BSA_Type11 type11:
                        CopyBsaType11References(type11, move);
                        break;
                    case BSA_Type12 type12:
                        CopyBsaType12References(type12, move);
                        break;
                }
            }
        }

        private void CopyBsaType0References(BSA_Type0 bsaType, Move move)
        {
            if (bsaType.BSA_EntryID == ushort.MaxValue) return;

            var entry = move.Files.BsaFile.File.BSA_Entries.FirstOrDefault(x => x.SortID == bsaType.BSA_EntryID);

            if (entry != null && !Secondary.BsaEntries.Any(x => (ushort)x.SortID == bsaType.BSA_EntryID))
            {
                Secondary.BsaEntries.Add(entry);
                CopyBsaEntryReferences(entry, move);
            }

            if(entry != null)
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.BSA_EntryID), ValueReference.InstanceRefType.Bsa));
        }

        private void CopyBsaType3References(BSA_Type3 bsaType, Move move)
        {
            if(CopyShotHitbox(bsaType.FirstHit, move))
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.FirstHit), ValueReference.InstanceRefType.ShotBdm));

            if(CopyShotHitbox(bsaType.MultipleHits, move))
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.MultipleHits), ValueReference.InstanceRefType.ShotBdm));

            if(CopyShotHitbox(bsaType.LastHit, move))
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.LastHit), ValueReference.InstanceRefType.ShotBdm));
        }

        private void CopyBsaType6References(BSA_Type6 bsaType, Move move)
        {
            if(CopyEffect((BAC_Type8.EepkTypeEnum)bsaType.EepkType, bsaType.EffectID, bsaType.SkillID, move))
            {
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.EffectID), ValueReference.InstanceRefType.Eepk));
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
            }
        }

        private void CopyBsaType7References(BSA_Type7 bsaType, Move move)
        {
            if (BsaSoundResources.TryGetBacAcbType(bsaType.AcbType, out Xv2CoreLib.BAC.AcbType acbType) &&
                CopyCue(acbType, bsaType.CueId, move))
            {
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.CueId), ValueReference.InstanceRefType.SeAcb));
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.AcbType), ValueReference.InstanceRefType.SeAcb, ValueReference.Mode.Type));
            }
        }

        private void CopyBsaType12References(BSA_Type12 bsaType, Move move)
        {
            //Nothing to copy: Type12 signals an existing skill, it does not spawn an effect. Only remap it when
            //it points at the move that was copied. Any other skill it names is deliberate and must stay.
            if (bsaType.SkillID != SkillID) return;

            ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
            ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
        }

        private void CopyBsaType11References(BSA_Type11 bsaType, Move move)
        {
            if (CopyEffect((BAC_Type8.EepkTypeEnum)bsaType.SkillType, bsaType.EffectID, bsaType.SkillID, move))
            {
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.EffectID), ValueReference.InstanceRefType.Eepk));
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.SkillType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
            }
        }

        private List<IUndoRedo> PasteBsaEntries(IList<BSA_Entry> bsaEntries, Move move, BSA_Entry bsaEntryToReplace = null)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (bsaEntryToReplace != null)
            {
                if (bsaEntries.Count == 0) return undos;

                BSA_Entry source = bsaEntries[0].Copy();

                //Replace the whole entry body. The target keeps its own ID and name so references to it stay valid.
                undos.AddRange(Utils.CopyValues(bsaEntryToReplace, source, nameof(BSA_Entry.Index), nameof(BSA_Entry.SortID), nameof(BSA_Entry.UserDefinedName)));

                undos.Add(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.IBsaTypes), bsaEntryToReplace, bsaEntryToReplace.IBsaTypes, source.IBsaTypes));
                bsaEntryToReplace.IBsaTypes = source.IBsaTypes;

                undos.Add(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.SubEntries), bsaEntryToReplace, bsaEntryToReplace.SubEntries, source.SubEntries));
                bsaEntryToReplace.SubEntries = source.SubEntries;

                undos.Add(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.I_40), bsaEntryToReplace, bsaEntryToReplace.I_40, source.I_40));
                bsaEntryToReplace.I_40 = source.I_40;

                ObjectExtensions.NotifyPropsChanged(bsaEntryToReplace);
                undos.Add(new UndoActionPropNotify(bsaEntryToReplace, true));
            }
            else
            {
                BSA_File bsaFile = move.Files.BsaFile.File;

                //Assign every new ID before copying anything. BSA entries chain to each other through
                //Expires, the Impact fields and Type0. ReplaceIdReference writes into the clipboard objects,
                //so the remap has to finish while those are still the objects the ValueRefs point at.
                List<int> takenIds = bsaFile.BSA_Entries.Select(entry => entry.SortID).ToList();
                List<int> newIds = new List<int>();

                foreach (var bsaEntry in bsaEntries)
                {
                    int newId = 0;

                    while (takenIds.Contains(newId))
                        newId++;

                    takenIds.Add(newId);
                    newIds.Add(newId);
                    ReplaceIdReference(ValueReference.InstanceRefType.Bsa, bsaEntry.SortID, newId);
                }

                for (int i = 0; i < bsaEntries.Count; i++)
                {
                    //Without the copy, a second paste of the same clipboard entry puts one shared instance in the file twice.
                    BSA_Entry bsaEntryCopy = bsaEntries[i].Copy();
                    bsaFile.AddEntry(newIds[i], bsaEntryCopy);
                    undos.Add(new UndoableListAdd<BSA_Entry>(bsaFile.BSA_Entries, bsaEntryCopy));
                }
            }

            return undos;
        }

    }
}
