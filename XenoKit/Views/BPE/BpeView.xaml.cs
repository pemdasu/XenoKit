using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EEPK_Organiser.Forms;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.ViewModel.BPE;
using Xv2CoreLib.BPE;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BpeView : UserControl, IDynamicTabObject, INotifyPropertyChanged
    {
        private readonly BPE_File file;
        private string path;
        private BpeKeyframeRow selectedRow;

        public event PropertyChangedEventHandler PropertyChanged;
        public string FilePath => path;
        public string DisplayName => GetSaveContextFileName();
        public BpePreviewController Preview { get; } = new BpePreviewController();

        private BPE_Entry Entry => entries?.SelectedItem as BPE_Entry;

        public List<BpeKeyframeRow> Rows { get; private set; } = new List<BpeKeyframeRow>();
        private BpeEntryViewModel entryViewModel;
        /// <summary>Entry fields show only while no keyframe row is selected, like the BSA editor.</summary>
        public BpeEntryViewModel EntryViewModel => SelectedRow == null ? entryViewModel : null;
        public BpeViewModelBase KeyframeViewModel { get; private set; }
        public BpeLayerViewModel LayerViewModel { get; private set; }
        public bool HasKeyframe => KeyframeViewModel != null;

        public BpeKeyframeRow SelectedRow
        {
            get => selectedRow;
            set
            {
                if (selectedRow == value) return;
                selectedRow = value;
                ShowRow();
            }
        }

        public RelayCommand AddEntryCommand => new RelayCommand(() => Run(AddEntry));
        public RelayCommand RemoveEntryCommand => new RelayCommand(() => Run(RemoveEntry), () => Entry != null);
        public RelayCommand DuplicateEntryCommand => new RelayCommand(() => Run(DuplicateEntry), () => Entry != null);
        public RelayCommand<int> AddKeyframeCommand => new RelayCommand<int>(type => Run(() => AddKeyframe(type)), type => Entry != null);
        public RelayCommand RemoveKeyframeCommand => new RelayCommand(() => Run(RemoveKeyframes), () => SelectedRow != null);
        public RelayCommand DuplicateKeyframeCommand => new RelayCommand(() => Run(DuplicateKeyframes), () => SelectedRow?.Keyframe != null);
        public RelayCommand HueAdjustCommand => new RelayCommand(() => Run(() => Recolor(false)), () => GetColorKeyframes().Count > 0);
        public RelayCommand HueSetCommand => new RelayCommand(() => Run(() => Recolor(true)), () => GetColorKeyframes().Count > 0);

        private List<BpeKeyframeRow> SelectedRows => rows.SelectedItems.Cast<BpeKeyframeRow>().ToList();

        public BpeView(BPE_File file, string path)
        {
            this.file = file;
            this.path = path;
            InitializeComponent();

            entries.SelectionChanged += async (sender, args) =>
            {
                Preview.SetEntry(Entry);
                ShowEntry();
                if (SceneManager.CurrentSceneState == EditorTabs.PostEffects)
                    await ActivatePreview();
            };
            Loaded += (sender, args) => { UndoManager.Instance.UndoOrRedoCalled += Undo; entries.Items.Refresh(); };
            Unloaded += (sender, args) => { UndoManager.Instance.UndoOrRedoCalled -= Undo; Preview.Clear(); };

            entries.ItemsSource = file.Entries;
            entries.SelectedIndex = file.Entries.Count > 0 ? 0 : -1;
        }

        private static void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) { Log.Add("BPE editor: " + exception.Message, LogType.Error); }
        }

        #region Display
        private void ShowEntry(object rowKeyframe = null, BPE_SubEntry rowLayer = null)
        {
            entryViewModel = Entry != null ? new BpeEntryViewModel(Entry, file, Changed) : null;
            Rows = BuildRows();
            Notify(nameof(Rows));

            selectedRow = Rows.FirstOrDefault(item => item.Keyframe != null ? item.Keyframe == rowKeyframe : rowLayer != null && item.Layer == rowLayer);
            ShowRow();
        }

        /// <summary>
        /// When you click the selected entry again, the keyframe selection clears and the entry fields show again.
        /// </summary>
        private void Entries_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(entries, e.OriginalSource as System.Windows.DependencyObject) is DataGridRow row && row.Item == Entry)
                SelectedRow = null;
        }

        private List<BpeKeyframeRow> BuildRows()
        {
            List<BpeKeyframeRow> rows = new List<BpeKeyframeRow>();

            if (Entry?.SubEntries == null)
                return rows;

            foreach (BPE_SubEntry layer in Entry.SubEntries)
            {
                IList keyframes = BpeKeyframeRow.GetKeyframes(layer);

                if (keyframes == null || keyframes.Count == 0)
                {
                    rows.Add(new BpeKeyframeRow(Entry, layer, keyframes, null, file, Changed));
                    continue;
                }

                foreach (object keyframe in keyframes)
                    rows.Add(new BpeKeyframeRow(Entry, layer, keyframes, keyframe, file, Changed));
            }

            return rows;
        }

        private void ShowRow()
        {
            KeyframeViewModel = SelectedRow != null ? BpeKeyframeViewModels.Create(SelectedRow.Keyframe, file, Changed) : null;
            LayerViewModel = SelectedRow != null ? new BpeLayerViewModel(SelectedRow.Layer, file, Changed) : null;
            Notify(nameof(SelectedRow));
            Notify(nameof(EntryViewModel));
            Notify(nameof(KeyframeViewModel));
            Notify(nameof(LayerViewModel));
            Notify(nameof(HasKeyframe));
        }

        private async void Changed()
        {
            entries.Items.Refresh();
            rows.Items.Refresh();
            //A layer or entry edit can change the duration of the selected row.
            SelectedRow?.RaisePropertyChanged(string.Empty);

            if (SceneManager.BpePreview == Preview) await ActivatePreview();
            else Preview.Refresh();
        }

        private void Undo(object sender, UndoEventRaisedEventArgs args)
        {
            if (args.UndoContext != file) return;

            entries.Items.Refresh();
            Preview.SetEntry(Entry);
            ShowEntry(SelectedRow?.Keyframe, SelectedRow?.Layer);
            Changed();
        }

        public async System.Threading.Tasks.Task ActivatePreview()
        {
            SceneManager.BpePreview = Preview;
            await SceneManager.AsyncEnsureActorIsSet(0);
            if (SceneManager.BpePreview != Preview || SceneManager.CurrentSceneState != EditorTabs.PostEffects) return;
            if (SceneManager.Actors[0] != null) SceneManager.Actors[0].IsVisible = true;
            Preview.Refresh();
        }

        private void Notify(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        #endregion

        #region Entry commands
        private int FindFreeEntryId()
        {
            HashSet<int> usedIds = new HashSet<int>(file.Entries.Select(entry => entry.SortID));
            return Enumerable.Range(0, short.MaxValue).First(id => !usedIds.Contains(id));
        }

        private void AddEntry() => InsertEntry(new BPE_Entry { I_04 = 60, SubEntries = new List<BPE_SubEntry>() }, "Add BPE entry");

        private void DuplicateEntry() => InsertEntry(Entry.Copy(), "Duplicate BPE entry");

        private void InsertEntry(BPE_Entry entry, string message)
        {
            entry.Index = FindFreeEntryId().ToString();
            file.Entries.Add(entry);
            UndoManager.Instance.AddUndo(new UndoableListAdd<BPE_Entry>(file.Entries, entry, message), UndoGroup.Default, undoContext: file);
            entries.Items.Refresh();
            entries.SelectedItem = entry;
        }

        private void RemoveEntry()
        {
            BPE_Entry entry = Entry;
            UndoManager.Instance.AddUndo(new UndoableListRemove<BPE_Entry>(file.Entries, entry, file.Entries.IndexOf(entry), "Remove BPE entry"), UndoGroup.Default, undoContext: file);
            file.Entries.Remove(entry);
            entries.Items.Refresh();
        }
        #endregion

        #region Keyframe commands
        /// <summary>
        /// Adds a keyframe of the type at the preview frame, to the first layer of that type. Makes the layer when the entry has none.
        /// </summary>
        private void AddKeyframe(int type)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            BPE_SubEntry layer = Entry.SubEntries.FirstOrDefault(item => (int)item.BpeType == type);

            if (layer == null)
            {
                layer = new BPE_SubEntry { BpeType = (BpeType)type, I_06 = Entry.I_04 };
                var listProperty = typeof(BPE_SubEntry).GetProperty("Type" + type);
                listProperty.SetValue(layer, Activator.CreateInstance(listProperty.PropertyType));
                Entry.SubEntries.Add(layer);
                undos.Add(new UndoableListAdd<BPE_SubEntry>(Entry.SubEntries, layer));
            }

            IList keyframes = BpeKeyframeRow.GetKeyframes(layer);
            int frame = Preview.Frame;
            int index = 0;

            while (index < keyframes.Count && BpeKeyframeRow.GetFrame(keyframes[index]) <= frame)
                index++;

            //A new keyframe starts with the values of the one before it, so the effect does not jump.
            object keyframe = index > 0 ? keyframes[index - 1].Copy() : Activator.CreateInstance(keyframes.GetType().GetGenericArguments()[0]);
            if (index == 0 && keyframe is BPE_Type9 outline) { outline.F_08 = 100; outline.F_20 = 1; outline.F_24 = 1; outline.F_28 = 1; outline.F_32 = 1; }
            BpeKeyframeRow.SetFrame(keyframe, frame);

            keyframes.Insert(index, keyframe);
            undos.Add(CreateInsertUndo(keyframes, index, keyframe));
            UndoManager.Instance.AddCompositeUndo(undos, "Add BPE keyframe", UndoGroup.Default, undoContext: file);
            ShowEntry(keyframe, layer);
            Changed();
        }

        private void DuplicateKeyframes()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            BpeKeyframeRow lastRow = null;
            object lastCopy = null;

            foreach (BpeKeyframeRow row in SelectedRows.Where(row => row.Keyframe != null))
            {
                object keyframe = row.Keyframe.Copy();
                int index = row.Keyframes.IndexOf(row.Keyframe) + 1;
                row.Keyframes.Insert(index, keyframe);
                undos.Add(CreateInsertUndo(row.Keyframes, index, keyframe));
                lastRow = row;
                lastCopy = keyframe;
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Duplicate BPE keyframe", UndoGroup.Default, undoContext: file);
            ShowEntry(lastCopy, lastRow?.Layer);
            Changed();
        }

        /// <summary>
        /// Removes the selected keyframes. A layer with no keyframes has no effect, so this also removes the layer.
        /// </summary>
        private void RemoveKeyframes()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (BpeKeyframeRow row in SelectedRows)
            {
                if (row.Keyframe != null)
                {
                    int index = row.Keyframes.IndexOf(row.Keyframe);
                    undos.Add(CreateListUndo(typeof(UndoableListRemove<>), row.Keyframe, row.Keyframes, row.Keyframe, index, ""));
                    row.Keyframes.RemoveAt(index);
                }

                if ((row.Keyframes == null || row.Keyframes.Count == 0) && Entry.SubEntries.Contains(row.Layer))
                {
                    undos.Add(new UndoableListRemove<BPE_SubEntry>(Entry.SubEntries, row.Layer, Entry.SubEntries.IndexOf(row.Layer)));
                    Entry.SubEntries.Remove(row.Layer);
                }
            }

            UndoManager.Instance.AddCompositeUndo(undos, "Remove BPE keyframe", UndoGroup.Default, undoContext: file);
            ShowEntry();
            Changed();
        }

        private List<object> GetColorKeyframes()
        {
            return SelectedRows.Select(row => row.Keyframe).Where(keyframe => keyframe != null && BpeColors.GetColorFields(keyframe) != null).ToList();
        }

        private void Recolor(bool hueSet)
        {
            RecolorAll recolor = new RecolorAll(file, GetColorKeyframes(), hueSet, Application.Current.MainWindow);

            if (recolor.Initialize())
                recolor.ShowDialog();
        }

        private static IUndoRedo CreateInsertUndo(IList list, int index, object item) => CreateListUndo(typeof(UndoableListInsert<>), item, list, index, item, null);

        //Each layer type has its own keyframe list type, so the undo type comes from the keyframe type.
        private static IUndoRedo CreateListUndo(Type undoType, object item, params object[] arguments)
        {
            return (IUndoRedo)Activator.CreateInstance(undoType.MakeGenericType(item.GetType()), arguments);
        }
        #endregion

        #region Save
        public string GetSaveContextFileName() => Path.GetFileName(path);
        public bool CanSave() => true;
        public void Save() => Run(() => { if (path == null) SaveAs(); else file.SaveBinary(path); });

        private void SaveAs()
        {
            SaveFileDialog dialog = new SaveFileDialog { Filter = "BPE effects|*.bpe", FileName = GetSaveContextFileName() ?? "cmn.bpe" };
            if (dialog.ShowDialog() != true) return;
            file.SaveBinary(dialog.FileName);
            path = dialog.FileName;
            Notify(nameof(DisplayName));
        }
        #endregion
    }
}
