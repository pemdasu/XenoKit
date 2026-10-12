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
    public partial class BacTab : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public static event EventHandler BacTypeSelectionChanged;
        public Files files => Files.Instance;

        public IList<Xv2File<BAC_File>> BacFiles
        {
            get
            {
                List<Xv2File<BAC_File>> bacFiles = new List<Xv2File<BAC_File>>();
                IList<Xv2File<BAC_File>> sourceFiles = files.SelectedMove?.Files?.BacFiles;

                if (sourceFiles == null)
                    return bacFiles;

                for (int i = 0; i < sourceFiles.Count; i++)
                {
                    Xv2File<BAC_File> file = sourceFiles[i];

                    if (i == 0 || HasPath(file))
                        bacFiles.Add(file);
                }

                return bacFiles;
            }
        }

        private BAC_Entry _selectedBacEntry = null;
        public BAC_Entry SelectedBacEntry
        {
            get => _selectedBacEntry;
            set
            {
                if(_selectedBacEntry != value) 
                {
                    _selectedBacEntry = value;
                    NotifyPropertyChanged(nameof(SelectedBacEntry));

                    if (SceneManager.IsOnTab(EditorTabs.Action))
                    {
                        PlayBacEntry();
                    }
                }
            }
        }
        private IList<BAC_Entry> SelectedBacEntries => bacEntryDataGrid.SelectedItems.Cast<BAC_Entry>().ToList();
        private IBacType SelectedBacType
        {
            get
            {
                if (ViewMode == BacViewMode.TimeLine)
                    return timeline.SelectedItem as IBacType;
                else
                    return bacTypeDataGrid.SelectedItem as IBacType;
            }
        }
        private IList<IBacType> SelectedBacTypes
        {
            get
            {
                if (ViewMode == BacViewMode.TimeLine)
                    return timeline.SelectedItems.Cast<IBacType>().ToList();
                else
                    return bacTypeDataGrid.SelectedItems.Cast<IBacType>().ToList();

            }
        }

        //Selected BacTypes exposed as statics for gizmos to check against
        public static IBacBone SelectedIBacBone { get; private set; }
        public static IBacType StaticSelectedBacType { get; set; }



        //List Filter
        private ListCollectionView _viewbacTypes = null;
        public ListCollectionView ViewBacTypes
        {
            get
            {
                return _viewbacTypes;
            }
            set
            {
                if (value != _viewbacTypes)
                {
                    _viewbacTypes = value;
                    NotifyPropertyChanged(nameof(ViewBacTypes));
                }
            }
        }

        public bool HideEmptryBacEntries
        {
            get
            {
                return SettingsManager.settings.XenoKit_HideEmptyBacEntries;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_HideEmptyBacEntries != value)
                {
                    SettingsManager.settings.XenoKit_HideEmptyBacEntries = value;
                    SettingsManager.Instance.SaveSettings();
                    ReapplyBacFilter();
                }
            }
        }

        private void CreateFilteredList()
        {
            if (files.SelectedItem?.SelectedBacFile != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ViewBacTypes = new ListCollectionView(files.SelectedItem.SelectedBacFile.File.BacEntries.Binding);
                    ViewBacTypes.Filter = new Predicate<object>(BacFilterCheck);
                }));
            }
            else
            {
                ViewBacTypes = null;
            }
        }

        private void ReapplyBacFilter()
        {
            if (ViewBacTypes != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ViewBacTypes.SortDescriptions.Add(new SortDescription(nameof(BAC_Entry.SortID), ListSortDirection.Ascending));
                    ViewBacTypes.Filter = new Predicate<object>(BacFilterCheck);
                    NotifyPropertyChanged(nameof(ViewBacTypes));

                    if (bacEntryDataGrid.SelectedItem != null)
                        bacEntryDataGrid.ScrollIntoView(bacEntryDataGrid.SelectedItem);
                }));
            }
        }

        private bool BacFilterCheck(object bacEntry)
        {
            if (bacEntry is BAC_Entry entry)
            {
                return SettingsManager.settings.XenoKit_HideEmptyBacEntries ? !entry.IsIBacEntryEmpty() || entry.NewEntry : true;
            }
            else
            {
                return true;
            }
        }

        //Visibilities
        public Visibility BacTypeListVisbility => (bacEntryDataGrid.SelectedItem is BAC_Entry) ? Visibility.Visible : Visibility.Hidden;
        public bool IsMovesetBac { get; set; }
        public bool IsMovesetOrCMNBac { get; set; }

        //BAC Type View
        private BacViewMode _viewMode = BacViewMode.TimeLine;
        public BacViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (_viewMode != value)
                {
                    _viewMode = value;
                    NotifyPropertyChanged(nameof(ViewMode));
                    NotifyPropertyChanged(nameof(IsDataGridMode));
                    NotifyPropertyChanged(nameof(IsTimeLineMode));
                    BacType_SelectionChanged(this, null);
                }
            }
        }
        public bool IsDataGridMode
        {
            get => _viewMode == BacViewMode.DataGrid;
            set
            {
                ViewMode = value ? BacViewMode.DataGrid : BacViewMode.TimeLine;
            }
        }
        public bool IsTimeLineMode
        {
            get => _viewMode == BacViewMode.TimeLine;
            set
            {
                ViewMode = value ? BacViewMode.TimeLine : BacViewMode.DataGrid;
            }
        }

        #region BacEntryID
        public int SelectedBacID
        {
            get => SelectedBacEntry != null ? SelectedBacEntry.SortID : -1;
            set
            {
                if (SelectedBacEntry != null && value >= 0)
                {
                    EditBacId(value);
                }

                NotifyPropertyChanged(nameof(SelectedBacID));
            }
        }

        private void EditBacId(int newId)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            var existing = Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries.FirstOrDefault(a => a.SortID == newId && a != SelectedBacEntry);

            if (existing?.IsIBacEntryEmpty() == true)
            {
                //An entry already exists with this ID but it is empty. We can safely remove it and just use the ID
                undos.Add(new UndoableListRemove<BAC_Entry>(Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries, existing));
                Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries.Remove(existing);
            }

            if (existing != null && undos.Count == 0)
            {
                MessagePrompt.Show("The entered ID is already used by another BAC entry.", "ID Already Used", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return;
            }
            else
            {
                undos.Add(new UndoableProperty<BAC_Entry>(nameof(BAC_Entry.SortID), SelectedBacEntry, SelectedBacEntry.SortID, newId));
                UndoManager.Instance.AddCompositeUndo(undos, "BAC ID", UndoGroup.Action, "ID", SelectedBacEntry);
                SelectedBacEntry.SortID = newId;
            }

            SelectedBacEntry.UpdateEntryName();
        }

        public int MaximumBacID
        {
            get
            {
                if (Files.Instance.SelectedMove != null)
                {
                    if (Files.Instance.SelectedMove.MoveType == Move.Type.Skill) return BAC_Entry.MAX_ENTRIES_SKILL - 1;
                    if (Files.Instance.SelectedMove.MoveType == Move.Type.Moveset || Files.Instance.SelectedMove.MoveType == Move.Type.CMN) return BAC_Entry.MAX_ENTRIES_CHARACTER - 1;
                }

                return 10000;
            }
        }

        #endregion

        public BacTab()
        {
            InitializeComponent();
            DataContext = this;
            NotifyPropertyChanged(nameof(files));
            Files.SelectedItemChanged += Files_SelectedMoveChanged;
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
            Engine.Animation.VisualSkeleton.SelectedBoneChanged += VisualSkeleton_SelectedBoneChanged;
        }

        #region Events
        private void VisualSkeleton_SelectedBoneChanged(object sender, EventArgs e)
        {
            if (SceneManager.IsOnTab(EditorTabs.Action) && SelectedIBacBone != null)
            {
                if (sender is int boneIdx)
                {
                    string boneName = SceneManager.Actors[0].Skeleton.Bones[boneIdx].Name;
                    BoneLinks boneLink;

                    if (Enum.TryParse(boneName, out boneLink))
                    {
                        if (SelectedIBacBone.BoneLink != boneLink)
                        {
                            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(IBacBone.BoneLink), SelectedIBacBone, SelectedIBacBone.BoneLink, boneLink, "Bone Link"), UndoGroup.Action, "BoneLink");
                            SelectedIBacBone.BoneLink = boneLink;
                            UndoManager.Instance.ForceEventCall(UndoGroup.Action, "BoneLink");
                        }
                    }
                }
            }
        }

        private void UndoManager_UndoOrRedoCalled(object sender, UndoEventRaisedEventArgs e)
        {
            if (e.UndoArg == "ID" && e.UndoContext is BAC_Entry bacEntry)
            {
                bacEntry.UpdateEntryName();
            }

            if (e.UndoGroup == UndoGroup.Action && SelectedBacEntry != null)
            {
                SelectedBacEntry.RefreshIBacTypes();

                foreach (var type in SelectedBacEntry.IBacTypes)
                {
                    type.RefreshType();
                }

                if (e.UndoArg == "BoneLink")
                {
                    TryEnableGizmos();
                }

            }
        }

        private void Files_SelectedMoveChanged(object sender, EventArgs e)
        {
            UpdateSelectedBacFile();
        }

        private void BacFileSelection_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedBacFile();
        }

        private void BacType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            UpdateViewModels();
            BacTypeSelectionChanged?.Invoke(this, new EventArgs());

            TryEnableGizmos();
            SetStaticBacTypes();
        }

        private void TimeLine_Toggled(object sender, RoutedEventArgs e)
        {
            //Going from DataGrid -> TimeLine we must sort the layers on the bac entry, assigning layers to any types added through the DataGrid add/paste methods.
            if (ViewMode == BacViewMode.DataGrid && SelectedBacEntry != null)
            {
                SelectedBacEntry.SortTimeLineLayers();
                timeline.BacEntryChanged();
            }

            ViewMode = timelineToggle.IsOn ? BacViewMode.TimeLine : BacViewMode.DataGrid;

        }

        private void bacEntryDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                var dataGrid = sender as DataGrid;

                if (dataGrid.CurrentCell.Column is DataGridTextColumn column && dataGrid.CurrentItem != null && column == userDefinedNameColumn)
                {
                    column.IsReadOnly = false;
                    dataGrid.BeginEdit();
                    e.Handled = true;
                }
            }
            else if(e.Key == Key.Enter)
            {
                var dataGrid = sender as DataGrid;

                if (dataGrid.CurrentCell.Column is DataGridTextColumn column && dataGrid.CurrentItem != null && column == userDefinedNameColumn)
                {
                    column.IsReadOnly = true;
                    dataGrid.CommitEdit();
                    e.Handled = true;
                }
            }
        }

        private void bacEntryDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column == userDefinedNameColumn)
            {
                userDefinedNameColumn.IsReadOnly = true;
            }
        }

        #endregion

        private void UpdateSelectedBacFile()
        {
            SelectCurrentMoveBacFile();
            NotifyPropertyChanged(nameof(BacFiles));
            NotifyPropertyChanged(nameof(BacTypeListVisbility));
            NotifyPropertyChanged(nameof(files));
            NotifyPropertyChanged(nameof(MaximumBacID));
            CreateFilteredList();

            if(files.SelectedItem != null)
            {
                IsMovesetOrCMNBac = (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.SelectedBacFile?.FileType == Xenoverse2.MoveFileTypes.AFTER_BAC || (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.CMN && bacFileComboBox.SelectedIndex == 0));
                IsMovesetBac = (files.SelectedItem.Type == OutlinerItem.OutlinerItemType.Character || files.SelectedItem.SelectedBacFile?.FileType == Xenoverse2.MoveFileTypes.AFTER_BAC);
                NotifyPropertyChanged(nameof(IsMovesetOrCMNBac));
                NotifyPropertyChanged(nameof(IsMovesetBac));
                
                cmnOverrideMenu.Visibility = IsMovesetBac ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SelectCurrentMoveBacFile()
        {
            if (files.SelectedItem == null)
                return;

            IList<Xv2File<BAC_File>> bacFiles = BacFiles;

            if (files.SelectedItem.SelectedBacFile == null || !bacFiles.Contains(files.SelectedItem.SelectedBacFile))
                files.SelectedItem.SelectedBacFile = bacFiles.FirstOrDefault();
        }

        private static bool HasPath<T>(Xv2File<T> file) where T : class
        {
            return !string.IsNullOrWhiteSpace(file?.Path);
        }

        private void UpdateViewModels()
        {
            NotifyPropertyChanged(nameof(BacType0ViewModel));
            NotifyPropertyChanged(nameof(BacType1ViewModel));
            NotifyPropertyChanged(nameof(BacType2ViewModel));
            NotifyPropertyChanged(nameof(BacType3ViewModel));
            NotifyPropertyChanged(nameof(BacType4ViewModel));
            NotifyPropertyChanged(nameof(BacType5ViewModel));
            NotifyPropertyChanged(nameof(BacType6ViewModel));
            NotifyPropertyChanged(nameof(BacType7ViewModel));
            NotifyPropertyChanged(nameof(BacType8ViewModel));
            NotifyPropertyChanged(nameof(BacType9ViewModel));
            NotifyPropertyChanged(nameof(BacType10ViewModel));
            NotifyPropertyChanged(nameof(BacType11ViewModel));
            NotifyPropertyChanged(nameof(BacType12ViewModel));
            NotifyPropertyChanged(nameof(BacType13ViewModel));
            NotifyPropertyChanged(nameof(BacType14ViewModel));
            NotifyPropertyChanged(nameof(BacType15ViewModel));
            NotifyPropertyChanged(nameof(BacType16ViewModel));
            NotifyPropertyChanged(nameof(BacType17ViewModel));
            NotifyPropertyChanged(nameof(BacType18ViewModel));
            NotifyPropertyChanged(nameof(BacType19ViewModel));
            NotifyPropertyChanged(nameof(BacType20ViewModel));
            NotifyPropertyChanged(nameof(BacType21ViewModel));
            NotifyPropertyChanged(nameof(BacType22ViewModel));
            NotifyPropertyChanged(nameof(BacType23ViewModel));
            NotifyPropertyChanged(nameof(BacType24ViewModel));
            NotifyPropertyChanged(nameof(BacType25ViewModel));
            NotifyPropertyChanged(nameof(BacType26ViewModel));
            NotifyPropertyChanged(nameof(BacType27ViewModel));
            NotifyPropertyChanged(nameof(BacType28ViewModel));
            NotifyPropertyChanged(nameof(BacType29ViewModel));
            NotifyPropertyChanged(nameof(BacType30ViewModel));
            NotifyPropertyChanged(nameof(BacType31ViewModel));
            NotifyPropertyChanged(nameof(BacTypeBaseViewModel));
        }

        public void TryEnableGizmos()
        {
            if (Viewport.Instance == null) return;

            if (SelectedBacType is BAC_Type1 hitbox)
            {
                Viewport.Instance.BacHitboxGizmo.SetContext(hitbox);
            }
            else
            {
                Viewport.Instance.BacHitboxGizmo.RemoveContext();
            }

            if (SelectedBacType is IBacTypeMatrix bacMatrix)
            {
                bool ignoreRotationOnBaseBone = SelectedBacType is BAC_Type1;
                bool rotationEnabled = SelectedBacType is BAC_Type8 || SelectedBacType is BAC_Type9;
                string boneName = null;

                if (SelectedBacType is IBacBone iBone)
                    boneName = iBone.BoneLink.ToString();

                //Movement is always on the base bone
                if (SelectedBacType is BAC_Type2)
                    boneName = Xv2CoreLib.ESK.ESK_File.BaseBone;

                Viewport.Instance.GetBacMatrixGizmo().SetContext(bacMatrix, true, rotationEnabled, false, boneName, ignoreRotationOnBaseBone, EditorTabs.Action);
            }
            else
            {
                Viewport.Instance.GetBacMatrixGizmo().RemoveContext();
            }
        }

        private void SetStaticBacTypes()
        {
            if (SelectedBacType is IBacBone bacBone)
            {
                SelectedIBacBone = bacBone;
            }
            else
            {
                SelectedIBacBone = null;
            }

            StaticSelectedBacType = SelectedBacType;
        }

        private void SelectBacEntry(BAC_Entry entry)
        {
            bacEntryDataGrid.SelectedItem = entry;
            bacEntryDataGrid.ScrollIntoView(entry);
        }

        public RelayCommand BacEntryRenameCommand => new RelayCommand(RenameBacEntry, IsBacEntrySelected);
        private void RenameBacEntry()
        {
            userDefinedNameColumn.IsReadOnly = false;
            bacEntryDataGrid.CurrentColumn = userDefinedNameColumn;
            bacEntryDataGrid.BeginEdit();
        }

        public void SetSelectedBacType(IBacType bacType)
        {
            timeline.SetSelectedItem(bacType, false);
            bacTypeDataGrid.SelectedItem = bacType;
            bacTypeDataGrid.ScrollIntoView(bacType);
        }

        public void AutoPlayBacEntry()
        {
            //Auto plays selected bac entry IF its not already active. Used for swapping tabs.

            if(SceneManager.Actors[0] != null)
            {
                if (!SceneManager.Actors[0].ActionControl.IsBacEntryActive(SelectedBacEntry))
                {
                    PlayBacEntry();
                }
            }
        }

    }

    public enum BacViewMode
    {
        DataGrid,
        TimeLine
    }
}
