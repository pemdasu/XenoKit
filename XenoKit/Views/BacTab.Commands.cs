using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using XenoKit.ViewModel.BAC;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.Resource.App;
using XenoKit.Windows;
using XenoKit.Engine;
using XenoKit.Editor;
using XenoKit.Windows.EAN;
using LB_Common.Forms;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for BacTab.xaml
    /// </summary>
    public partial class BacTab
    {
        #region BacEntryCommands
        public RelayCommand PlayBacEntryCommand => new RelayCommand(PlayBacEntry, IsBacEntrySelected);
        public void PlayBacEntry()
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            if (SelectedBacEntry != null && files.SelectedItem?.SelectedBacFile != null)
            {
                SceneManager.PlayBacEntry(files.SelectedItem.SelectedBacFile.File, SelectedBacEntry, files.SelectedMove, 0, true);
            }
        }

        public RelayCommand AddBacEntryCommand => new RelayCommand(AddBacEntry, IsBacFileLoaded);
        private void AddBacEntry()
        {
            BAC_Entry newBacEntry = Files.Instance.SelectedItem.SelectedBacFile.File.AddNewEntry();
            newBacEntry.NewEntry = true;
            UndoManager.Instance.AddUndo(new UndoableListAdd<BAC_Entry>(Files.Instance.SelectedMove.Files.BacFile.File.BacEntries, newBacEntry, "Bac Entry Add"));

            ReapplyBacFilter();
            SelectBacEntry(newBacEntry);

            Log.Add("BAC entry added with ID: " + newBacEntry.SortID);
        }

        public RelayCommand AddBacEntryAtSpecificIDCommand => new RelayCommand(AddBacEntryAtSpecificID, IsBacFileLoaded);
        private void AddBacEntryAtSpecificID()
        {
            ValueSelector valueSel = new ValueSelector("Add Entry...", "ID", null, files.SelectedItem.SelectedBacFile.File.GetFreeId(), 0, MaximumBacID);
            valueSel.ShowDialog();

            if (valueSel.IsFinished)
            {
                BAC_Entry entry = files.SelectedItem.SelectedBacFile.File.GetEntry(valueSel.Parameter);

                if(entry != null)
                {
                    if (!entry.NewEntry && entry.IsIBacEntryEmpty())
                    {
                        //If entry is empty, then force it to always appear regardless of Hide Empty setting
                        entry.NewEntry = true;
                        ReapplyBacFilter();
                    }

                    Log.Add($"Action Entry at ID {valueSel.Parameter} already exists!");
                }
                else
                {
                    entry = new BAC_Entry();
                    entry.NewEntry = true;
                    Files.Instance.SelectedItem.SelectedBacFile.File.AddEntry(entry, valueSel.Parameter);

                    UndoManager.Instance.AddUndo(new UndoableListAdd<BAC_Entry>(Files.Instance.SelectedMove.Files.BacFile.File.BacEntries, entry, "Bac Entry Add"));
                    ReapplyBacFilter();
                }

                Log.Add("BAC entry added with ID: " + entry.SortID);
                SelectBacEntry(entry);
            }
        }

        public RelayCommand OverrideCMNCommand => new RelayCommand(OverrideCMNBacEntry, IsBacFileLoaded);
        private void OverrideCMNBacEntry()
        {
            List<Item> ids = new List<Item>();

            foreach(BAC_Entry bacEntry in files.GetCmnMove().Files.BacFile.File.BacEntries)
            {
                if(files.SelectedMove.Files.BacFile.File.GetEntry(bacEntry.SortID) == null)
                {
                    ids.Add(new Item(bacEntry.SortID, bacEntry.UserDefinedName));
                }
            }

            ItemSelector selector = new ItemSelector(ids, "CMN Actions", true);
            selector.SetBooleanParameter("Copy Entry", "Copy the original CMN entry to use as a base.");
            selector.ShowDialog();

            if(selector.SelectedItem != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                BAC_Entry bacEntryToFocus = null;

                foreach(Item selectedItem in selector.SelectedItems)
                {
                    //AddEntry replaces an existing entry at this ID. The undo below only records an add, so the
                    //replaced entry cannot be recovered. Skip it rather than overwrite the user's work.
                    if (Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries.Any(existing => existing.SortID == selectedItem.ID))
                    {
                        Log.Add($"Action Entry at ID {selectedItem.ID} already exists and was not overridden.", LogType.Warning);
                        continue;
                    }

                    BAC_Entry cmnEntry = files.GetCmnMove().Files.BacFile.File.GetEntry(selectedItem.ID);
                    BAC_Entry bacEntry = selector.BooleanParameter ? cmnEntry.Copy() : new BAC_Entry();
                    bacEntry.NewEntry = true;

                    Files.Instance.SelectedItem.SelectedBacFile.File.AddEntry(bacEntry, selectedItem.ID);
                    undos.Add(new UndoableListAdd<BAC_Entry>(Files.Instance.SelectedMove.Files.BacFile.File.BacEntries, bacEntry));

                    if (bacEntryToFocus == null)
                        bacEntryToFocus = bacEntry;
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Override CMN Action");

                ReapplyBacFilter();
                SelectBacEntry(bacEntryToFocus);
            }
        }

        public RelayCommand RemoveBacEntryCommand => new RelayCommand(RemoveBacEntry, IsBacEntrySelected);
        private void RemoveBacEntry()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var entry in SelectedBacEntries)
            {
                SceneManager.ForceStopBacPlayer();
                undos.Add(new UndoableListRemove<BAC_Entry>(Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries, entry));
                Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries.Remove(entry);
            }

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Bac Entry Remove"));
        }

        public RelayCommand CopyBacEntryCommand => new RelayCommand(CopyBacEntry, IsBacEntrySelected);
        private void CopyBacEntry()
        {
            CopyItem copyItem = new CopyItem(SelectedBacEntries, Files.Instance.SelectedMove);
            XenoKitClipboard.SetData(ClipboardConstants.BacEntry_CopyItem, copyItem);
        }

        public RelayCommand PasteBacEntryCommand => new RelayCommand(PasteBacEntry, CanPasteBacEntries);
        private void PasteBacEntry()
        {
            if (!XenoKitClipboard.TryGetData(ClipboardConstants.BacEntry_CopyItem, out CopyItem copyItem))
                return;
            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove);
            pasteWindow.ShowDialog();
        }

        public RelayCommand PasteReplaceBacEntryCommand => new RelayCommand(PasteReplaceBacEntry, CanPasteReplaceBacEntries);
        private void PasteReplaceBacEntry()
        {
            if (!XenoKitClipboard.TryGetData(ClipboardConstants.BacEntry_CopyItem, out CopyItem copyItem))
                return;

            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove, SelectedBacEntry, true);
            pasteWindow.ShowDialog();

            UndoManager.Instance.ForceEventCall(UndoGroup.Action);
        }

        public RelayCommand EditBacFlagsCommand => new RelayCommand(EditBacFlags, IsBacFileLoaded);
        private void EditBacFlags()
        {
            Windows.BAC.EditBacEntryFlags form = new Windows.BAC.EditBacEntryFlags(SelectedBacEntry, System.Windows.Application.Current.MainWindow);
            form.ShowDialog();
        }

        public RelayCommand RebaseBacEntryCommand => new RelayCommand(RebaseBacEntry, IsBacEntrySelected);
        private void RebaseBacEntry()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (SelectedBacEntry != null)
            {
                EanModiferForm form = new EanModiferForm("Action Rebase");
                form.StartFrameEnabled = true;
                form.RebaseAmountEnabled = true;
                form.StartFrameConstraintEnabled = false;
                form.ShowDialog();

                if (form.Success)
                {
                    foreach (var type in SelectedBacEntry.IBacTypes)
                    {
                        if (type.StartTime >= form.StartFrame)
                        {
                            ushort newStartTime = (ushort)(type.StartTime + form.RebaseAmount);
                            undos.Add(new UndoableProperty<IBacType>(nameof(type.StartTime), type, type.StartTime, newStartTime));
                            type.StartTime = newStartTime;
                        }
                    }

                    UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Bac Entry Rebase"));
                }
            }
        }

        //Bools
        private bool IsBacFileLoaded()
        {
            if (Files.Instance.SelectedMove != null)
            {
                return Files.Instance.SelectedMove.Files.BacFile != null;
            }
            else
            {
                return false;
            }
        }

        private bool IsBacEntrySelected()
        {
            return bacEntryDataGrid.SelectedItem is BAC_Entry;
        }

        private bool IsBacTypeSelected()
        {
            return bacTypeDataGrid.SelectedItem is IBacType;
        }

        private bool CanFocus()
        {
            return SelectedBacType != null && SceneManager.Actors[0] != null && !Viewport.Instance.IsPlaying;
        }

        private bool CanPasteBacEntries()
        {
            return XenoKitClipboard.ContainsData(ClipboardConstants.BacEntry_CopyItem) && IsBacFileLoaded();
        }

        private bool CanPasteReplaceBacEntries()
        {
            return XenoKitClipboard.ContainsData(ClipboardConstants.BacEntry_CopyItem) && IsBacFileLoaded() && IsBacEntrySelected();
        }

        private bool CanPasteBacTypes()
        {
            return XenoKitClipboard.ContainsData(ClipboardConstants.BacType_CopyItem) && IsBacEntrySelected();
        }
        #endregion

        #region BacTypeListCommands
        public RelayCommand<int> AddBacTypeCommand => new RelayCommand<int>(AddBacType);
        private void AddBacType(int bacType)
        {
            if (!IsBacEntrySelected()) return;
            bacTypeDataGrid.SelectedItem = SelectedBacEntry.UndoableAddIBacType(bacType);
            bacTypeDataGrid.ScrollIntoView(bacTypeDataGrid.SelectedItem);
            SceneManager.InvokeBacDataChangedEvent();
        }

        public RelayCommand RemoveBacTypeCommand => new RelayCommand(RemoveBacType, IsBacTypeSelected);
        private void RemoveBacType()
        {
            SelectedBacEntry.UndoableRemoveIBacType(SelectedBacTypes);
            SceneManager.InvokeBacDataChangedEvent();
        }

        public RelayCommand CopyBacTypeCommand => new RelayCommand(CopyBacType, IsBacTypeSelected);
        private void CopyBacType()
        {
            CopyItem copyItem = new CopyItem(SelectedBacTypes, Files.Instance.SelectedMove);
            XenoKitClipboard.SetData(ClipboardConstants.BacType_CopyItem, copyItem);
        }

        public RelayCommand PasteBacTypeCommand => new RelayCommand(PasteBacType, CanPasteBacTypes);
        private void PasteBacType()
        {
            if (!XenoKitClipboard.TryGetData(ClipboardConstants.BacType_CopyItem, out CopyItem copyItem))
                return;

            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove, SelectedBacEntry, false);
            pasteWindow.ShowDialog();
        }

        public RelayCommand DuplicateBacTypeCommand => new RelayCommand(DuplicateBacType, IsBacTypeSelected);
        private void DuplicateBacType()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var type in SelectedBacTypes)
            {
                undos.Add(SelectedBacEntry.AddEntry(type.Copy()));
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Duplicate BacType");
        }

        public RelayCommand FocusBacTypeCommand => new RelayCommand(FocusBacType, CanFocus);
        private void FocusBacType()
        {
            SceneManager.Actors[0].ActionControl.BacPlayer.Seek(SelectedBacType.StartTime);
        }


        //Placeholder until Drag and Drop is implemented
        public RelayCommand MoveDownBacTypeCommand => new RelayCommand(MoveDownBacType, IsBacTypeSelected);
        private void MoveDownBacType()
        {
            if (SelectedBacType != null)
            {
                int idx = SelectedBacEntry.IBacTypes.IndexOf(SelectedBacType);

                if (idx < SelectedBacEntry.IBacTypes.Count - 1)
                {
                    SelectedBacEntry.IBacTypes.Move(idx, idx + 1);
                    UndoManager.Instance.AddUndo(new UndoableListMove<IBacType>(SelectedBacEntry.IBacTypes, idx, idx + 1, "BacType Move Up"));
                }
            }
        }

        public RelayCommand MoveUpBacTypeCommand => new RelayCommand(MoveUpBacType, IsBacTypeSelected);
        private void MoveUpBacType()
        {
            if (SelectedBacType != null)
            {
                int idx = SelectedBacEntry.IBacTypes.IndexOf(SelectedBacType);

                if (idx > 0)
                {
                    SelectedBacEntry.IBacTypes.Move(idx, idx - 1);
                    UndoManager.Instance.AddUndo(new UndoableListMove<IBacType>(SelectedBacEntry.IBacTypes, idx, idx - 1, "BacType Move Down"));
                }
            }
        }

        #endregion


    }
}
