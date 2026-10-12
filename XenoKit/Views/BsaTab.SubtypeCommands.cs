using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.ViewModel.BSA;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BsaTab : UserControl, INotifyPropertyChanged
    {
        // Subtype reorder commands were removed. The BSA format writes one header per type, so cross-type
        // order cannot round-trip. AddIBsaType keeps the list in TypeID order, matching what gets saved.

        public RelayCommand<string> AddSubtypeCommand => new RelayCommand<string>(AddSubtype, _ => SelectedEntry != null);
        public RelayCommand DuplicateSubtypeCommand => new RelayCommand(DuplicateSubtype, () => SelectedSubtypeRow != null);
        public RelayCommand CopySubtypeCommand => new RelayCommand(CopySubtype, () => SelectedSubtypeRow != null);
        public RelayCommand PasteSubtypeCommand => new RelayCommand(PasteSubtype, CanPasteSubtype);
        public RelayCommand DeleteSubtypeCommand => new RelayCommand(DeleteSubtype, () => SelectedSubtypeRow != null);

        private void AddSubtype(string typeText)
        {
            if (SelectedEntry == null) return;
            InitSubEntries();

            if (typeText == "Collision")
            {
                BSA_Collision collision = new BSA_Collision();
                SelectedEntry.SubEntries.CollisionEntries.Add(collision);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, collision, "BSA Collision Add"));
                SelectSubtypeSource(collision);
                return;
            }

            if (typeText == "CollisionSound")
            {
                BSA_CollisionSound collisionSound = new BSA_CollisionSound();
                SelectedEntry.SubEntries.CollisionSoundEntries.Add(collisionSound);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_CollisionSound>(SelectedEntry.SubEntries.CollisionSoundEntries, collisionSound, "BSA Collision Sound Add"));
                SelectSubtypeSource(collisionSound);
                return;
            }

            if (!int.TryParse(typeText, out int typeId)) return;

            IBsaType type = CreateBsaType(typeId);
            if (type == null) return;

            UndoManager.Instance.AddUndo(SelectedEntry.AddIBsaType(type));
            SelectSubtypeSource(type);
        }

        private void DuplicateSubtype()
        {
            if (SelectedEntry == null || SelectedSubtypeSource == null) return;
            InitSubEntries();

            switch (SelectedSubtypeSource)
            {
                case IBsaType type:
                    {
                        IBsaType clone = type.Copy();
                        UndoManager.Instance.AddUndo(SelectedEntry.AddIBsaType(clone));
                        SelectSubtypeSource(clone);
                        break;
                    }
                case BSA_Collision collision:
                    {
                        BSA_Collision clone = collision.Copy();
                        SelectedEntry.SubEntries.CollisionEntries.Add(clone);
                        UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, clone, "BSA Collision Duplicate"));
                        SelectSubtypeSource(clone);
                        break;
                    }
                case BSA_CollisionSound collisionSound:
                    {
                        BSA_CollisionSound clone = collisionSound.Copy();
                        SelectedEntry.SubEntries.CollisionSoundEntries.Add(clone);
                        UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_CollisionSound>(SelectedEntry.SubEntries.CollisionSoundEntries, clone, "BSA Collision Sound Duplicate"));
                        SelectSubtypeSource(clone);
                        break;
                    }
            }
        }

        private void CopySubtype()
        {
            switch (SelectedSubtypeSource)
            {
                case IBsaType type when files.SelectedMove != null:
                    XenoKitClipboard.SetData(ClipboardConstants.BsaType_CopyItem, new CopyItem(new List<IBsaType> { type }, files.SelectedMove));
                    break;
                case BSA_Collision collision:
                    XenoKitClipboard.SetData(ClipboardConstants.BsaCollision_CopyItem, collision.Copy());
                    break;
                case BSA_CollisionSound collisionSound:
                    XenoKitClipboard.SetData(ClipboardConstants.BsaCollisionSound_CopyItem, collisionSound.Copy());
                    break;
            }
        }

        private void PasteSubtype()
        {
            if (SelectedEntry == null) return;
            InitSubEntries();

            if (XenoKitClipboard.TryGetData(ClipboardConstants.BsaCollision_CopyItem, out BSA_Collision collision))
            {
                //Copy, or pasting twice puts one shared instance in the list twice.
                BSA_Collision collisionCopy = collision.Copy();
                SelectedEntry.SubEntries.CollisionEntries.Add(collisionCopy);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, collisionCopy, "BSA Collision Paste"));
                SelectSubtypeSource(collisionCopy);
                return;
            }

            if (XenoKitClipboard.TryGetData(ClipboardConstants.BsaCollisionSound_CopyItem, out BSA_CollisionSound collisionSound))
            {
                BSA_CollisionSound collisionSoundCopy = collisionSound.Copy();
                SelectedEntry.SubEntries.CollisionSoundEntries.Add(collisionSoundCopy);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_CollisionSound>(SelectedEntry.SubEntries.CollisionSoundEntries, collisionSoundCopy, "BSA Collision Sound Paste"));
                SelectSubtypeSource(collisionSoundCopy);
                return;
            }

            if (files.SelectedMove != null && XenoKitClipboard.TryGetData(ClipboardConstants.BsaType_CopyItem, out CopyItem copyItem))
            {
                new PasteCopyItem(copyItem, files.SelectedMove, SelectedEntry).ShowDialog();
                RebuildSubtypeRows();
            }
        }

        private void DeleteSubtype()
        {
            if (SelectedEntry == null) return;
            InitSubEntries();

            IList<object> sources = SelectedSubtypeSources;

            if (sources.Count == 0) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (object source in sources)
            {
                switch (source)
                {
                    case IBsaType type:
                        undos.Add(new UndoableListRemove<IBsaType>(SelectedEntry.IBsaTypes, type));
                        SelectedEntry.IBsaTypes.Remove(type);
                        break;
                    case BSA_Collision collision:
                        undos.Add(new UndoableListRemove<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, collision));
                        SelectedEntry.SubEntries.CollisionEntries.Remove(collision);
                        break;
                    case BSA_CollisionSound collisionSound:
                        undos.Add(new UndoableListRemove<BSA_CollisionSound>(SelectedEntry.SubEntries.CollisionSoundEntries, collisionSound));
                        SelectedEntry.SubEntries.CollisionSoundEntries.Remove(collisionSound);
                        break;
                }
            }

            UndoManager.Instance.AddCompositeUndo(undos, sources.Count > 1 ? "BSA Subtypes Delete" : "BSA Subtype Delete");
            SelectedSubtypeRow = null;
            RebuildSubtypeRows();
            PlaySelectedEntryPreview();
        }
    }
}
