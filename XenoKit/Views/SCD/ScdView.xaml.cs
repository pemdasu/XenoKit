using System;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.SCD;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class ScdView : UserControl
    {
        private readonly Xv2PartSetFile file;
        private readonly Actor actor;
        private readonly SCD_File scd;
        public Actor Actor => actor;
        public Xv2PartSetFile File => file;
        private string search = "";
        private SCD_Block SelectedBlock => blocks?.SelectedValue as SCD_Block;
        public Dictionary<string, string> BlockTypes { get; }
        public RelayCommand<string> AddBlockCommand => new RelayCommand<string>(AddBlock);
        public RelayCommand RemoveBlockCommand => new RelayCommand(RemoveBlock, () => SelectedBlock != null);
        public RelayCommand DuplicateBlockCommand => new RelayCommand(DuplicateBlock, () => SelectedBlock != null);
        public static readonly DependencyProperty BlockViewModelProperty = DependencyProperty.Register(
            nameof(BlockViewModel), typeof(object), typeof(ScdView));
        public object BlockViewModel
        {
            get => GetValue(BlockViewModelProperty);
            private set => SetValue(BlockViewModelProperty, value);
        }
        public ScdView(Xv2PartSetFile file, Actor actor)
        {
            this.file = file;
            this.actor = actor;
            scd = (SCD_File)file.File;
            BlockTypes = new Dictionary<string, string>
            {
                { "CFG0", "Physics speed" }, { "CFG1", "Physics motion" },
                { "CSR1", "Joint (CSR1)" }, { "CSR2", "Joint (CSR2)" }, { "ACT0", "Spring" }
            };
            if (scd.IsVersion1)
            {
                BlockTypes.Add("BDY3", "Body (BDY3)");
                BlockTypes.Add("BDY4", "Body (BDY4)");
                BlockTypes.Add("BDY5", "Body (BDY5)");
            }
            else
            {
                BlockTypes.Add("BDY0", "Body (BDY0)");
                BlockTypes.Add("BDY1", "Body (BDY1)");
            }
            InitializeComponent();
            RefreshBlocks();
            blocks.SelectionChanged += (sender, args) => ShowBlock();
            Loaded += (sender, args) =>
            {
                UndoManager.Instance.UndoOrRedoCalled += Undo;
                ActivatePreview();
                ShowBlock();
            };
            Unloaded += (sender, args) =>
            {
                UndoManager.Instance.UndoOrRedoCalled -= Undo;
                if (SceneManager.ScdEditor == this) SceneManager.ScdEditor = null;
            };
            blocks.SelectedIndex = scd.Blocks.Count > 0 ? 0 : -1;
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs args)
        {
            if (blocks.ItemsSource == null) return;
            search = ((TextBox)sender).Text;
            ApplySearch();
        }

        private void ApplySearch()
        {
            System.Windows.Data.CollectionViewSource.GetDefaultView(blocks.ItemsSource).Filter = item =>
                ((KeyValuePair<SCD_Block, string>)item).Value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Save_Click(object sender, RoutedEventArgs args) => Save();

        private void Undo(object sender, UndoEventRaisedEventArgs args)
        {
            if (args.UndoContext != scd) return;
            RefreshPhysics();
            RefreshBlocks();
            ShowBlock();
        }

        private void RefreshPhysics()
        {
            ActivatePreview();
            file.IsEdited = true;
            actor?.PartSet?.ReloadScdPhysics();
        }

        public void ActivatePreview()
        {
            Engine.Animation.ScdPhysics.Enabled = true;
            SceneManager.ScdEditor = this;
            if (actor != null && Viewport.Instance != null)
            {
                if (SceneManager.Actors[0] != actor) SceneManager.SetActor(actor, 0);
                SceneManager.ActorsEnable[0] = true;
            }
        }

        private void RefreshBlocks(SCD_Block selected = null)
        {
            selected = selected ?? SelectedBlock;
            blocks.ItemsSource = scd.Blocks.Select((block, index) => new KeyValuePair<SCD_Block, string>(block,
                $"{index}: {block.Tag} {block.Body?.Bone ?? block.Constraint?.BoneA ?? block.Spring?.BoneA}")).ToList();
            ApplySearch();
            blocks.SelectedValue = selected;
        }

        private void AddBlock(string tag)
        {
            SCD_Block block = new SCD_Block { Tag = tag };
            switch (tag)
            {
                case "CFG0": block.Config0 = new SCD_Config0 { MaxSpeed = 10 }; break;
                case "CFG1": block.Config1 = new SCD_Config1 { MaxSpeed = 10 }; break;
                case "BDY3": block.Body3 = new SCD_Body3(); break;
                case "BDY4": block.Body4 = new SCD_Body4(); break;
                case "BDY5": block.Body5 = new SCD_Body5(); break;
                case "BDY0":
                case "BDY1": block.BodyV2 = new SCD_BodyV2 { GravityFactor = 1, TimeFactor = 1 }; break;
                case "CSR1":
                case "CSR2": block.Constraint = new SCD_Constraint { BoneA = "", BoneB = "", FixedSize = 0xA0 }; break;
                case "ACT0": block.Spring = new SCD_Spring { BoneA = "", BoneB = "" }; break;
                default: throw new ArgumentOutOfRangeException(nameof(tag));
            }
            if (block.Body != null)
            {
                block.Body.Bone = "";
                block.Body.ShapeType = SCD_ShapeType.Sphere;
                block.Body.ShapeDim0 = 0.05f;
                block.Body.Mass = 1;
                block.Body.MotionType = scd.IsVersion1 ? 0 : 2;
                block.Body.InertiaMultipliers = new SCD_Vector3 { X = 1, Y = 1, Z = 1 };
            }
            scd.Blocks.Add(block);
            UndoManager.Instance.AddUndo(new UndoableListAdd<SCD_Block>(scd.Blocks, block, "Add SCD block"), UndoGroup.Default, undoContext: scd);
            RefreshBlocks(block); RefreshPhysics();
        }

        private void RemoveBlock()
        {
            SCD_Block block = SelectedBlock;
            UndoManager.Instance.AddUndo(new UndoableListRemove<SCD_Block>(scd.Blocks, block, scd.Blocks.IndexOf(block), "Remove SCD block"), UndoGroup.Default, undoContext: scd);
            scd.Blocks.Remove(block);
            RefreshBlocks(); RefreshPhysics();
        }

        private void DuplicateBlock()
        {
            SCD_Block block = SelectedBlock.Copy();
            scd.Blocks.Insert(scd.Blocks.IndexOf(SelectedBlock) + 1, block);
            UndoManager.Instance.AddUndo(new UndoableListAdd<SCD_Block>(scd.Blocks, block, "Duplicate SCD block"), UndoGroup.Default, undoContext: scd);
            RefreshBlocks(block); RefreshPhysics();
        }

        private void ShowBlock()
        {
            BlockViewModel = blocks.SelectedItem is KeyValuePair<SCD_Block, string> selected
                ? new ViewModel.SCD.ScdBlockViewModel(selected.Key, scd, RefreshPhysics)
                : null;
        }

        public bool CanSave() => file.IsLoaded;
        public void Save() => file.Save();
    }
}
