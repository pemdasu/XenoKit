using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine.Model;
using XenoKit.Helper;
using XenoKit.ViewModel.EMD;
using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource.UndoRedo;
using EEPK_Organiser.Forms;
using EEPK_Organiser.View;
using GalaSoft.MvvmLight.CommandWpf;
using XenoKit.Engine;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Animation;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Collections;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;
using SimdQuaternion = System.Numerics.Quaternion;
using Xv2CoreLib.Resource;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for ModelScene.xaml
    /// </summary>
    public partial class ModelSceneView : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public ModelScene ModelScene { get; private set; }
        public EMD_File EmdFile => ModelScene.Model.Type == ModelType.Nsk ? ModelScene.Model.SourceNskFile.EmdFile : ModelScene.Model.SourceEmdFile;

        public ObservableCollection<object> SelectedItems => ModelScene.SelectedItems;
        public object SelectedItem => SelectedItems.Count > 0 ? SelectedItems[0] : null;
        public EMD_Model SelectedModel => SelectedItem as EMD_Model;
        public EMD_Mesh SelectedMesh => SelectedItem as EMD_Mesh;
        public EMD_Submesh SelectedSubmesh => SelectedItem as EMD_Submesh;
        public EMD_TextureSamplerDef SelectedTexture => SelectedItem as EMD_TextureSamplerDef;

        public EmdTextureViewModel TextureViewModel { get; set; }

        //Names
        public string SelectedModelName
        {
            get => SelectedModel?.Name;
            set
            {
                if (SelectedModel != null && SelectedModel?.Name != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedModel.Name), SelectedModel, SelectedModel.Name, value, "Model Name"));
                    SelectedModel.Name = value;
                    NotifyPropertyChanged(nameof(SelectedModelName));
                    SelectedModel.RefreshValues();
                }
            }
        }
        public string SelectedMeshName
        {
            get => SelectedMesh?.Name;
            set
            {
                if (SelectedMesh != null && SelectedMesh?.Name != value)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(SelectedMesh.Name), SelectedMesh, SelectedMesh.Name, value, "Mesh Name"));
                    SelectedMesh.Name = value;
                    NotifyPropertyChanged(nameof(SelectedMeshName));
                    SelectedMesh.RefreshValues();
                }
            }
        }
        public string SelectedSubmeshName
        {
            get => SelectedSubmesh?.Name;
            set
            {
                if (SelectedSubmesh != null && SelectedSubmesh?.Name != value)
                {
                    UndoManager.Instance.AddCompositeUndo(new List<IUndoRedo>()
                    {
                        new UndoablePropertyGeneric(nameof(SelectedSubmesh.Name), SelectedSubmesh, SelectedSubmesh.Name, value),
                        new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Material, SelectedSubmesh))
                    }, "Submesh Name", UndoGroup.EMD);

                    SelectedSubmesh.Name = value;
                    NotifyPropertyChanged(nameof(SelectedSubmeshName));
                    SelectedSubmesh.RefreshValues();
                    EmdFile.TriggerModelModifiedEvent(EditTypeEnum.Material, SelectedSubmesh, null);
                }
            }
        }

        //Visibilities
        public Visibility ModelNameVisibility => SelectedModel != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MeshNameVisibility => SelectedMesh != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SubmeshNameVisibility => SelectedSubmesh != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TransformVisibility => SelectedModel != null || SelectedMesh != null || SelectedSubmesh != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TextureVisibility => SelectedTexture != null ? Visibility.Visible : Visibility.Collapsed;

        //Pos/Rot/Scale deltas
        private SimdVector3 _currentPos = SimdVector3.Zero;
        private SimdVector3 _currentRot = SimdVector3.Zero;
        private SimdVector3 _currentScale = SimdVector3.One;

        public float PosX
        {
            get => _currentPos.X;
            set => DoDirectTransform(new SimdVector3(value, _currentPos.Y, _currentPos.Z), _currentRot, _currentScale);
        }
        public float PosY
        {
            get => _currentPos.Y;
            set => DoDirectTransform(new SimdVector3(_currentPos.X, value, _currentPos.Z), _currentRot, _currentScale);
        }
        public float PosZ
        {
            get => _currentPos.Z;
            set => DoDirectTransform(new SimdVector3(_currentPos.X, _currentPos.Y, value), _currentRot, _currentScale);
        }
        public float RotX
        {
            get => _currentRot.X;
            set => DoDirectTransform(_currentPos, new SimdVector3(value, _currentRot.Y, _currentRot.Z), _currentScale);
        }
        public float RotY
        {
            get => _currentRot.Y;
            set => DoDirectTransform(_currentPos, new SimdVector3(_currentRot.X, value, _currentRot.Z), _currentScale);
        }
        public float RotZ
        {
            get => _currentRot.Z;
            set => DoDirectTransform(_currentPos, new SimdVector3(_currentRot.X, _currentRot.Y, value), _currentScale);
        }
        public float ScaleX
        {
            get => _currentScale.X;
            set => DoDirectTransform(_currentPos, _currentRot, new SimdVector3(value, _currentScale.Y, _currentScale.Z));
        }
        public float ScaleY
        {
            get => _currentScale.Y;
            set => DoDirectTransform(_currentPos, _currentRot, new SimdVector3(_currentScale.X, value, _currentScale.Z));
        }
        public float ScaleZ
        {
            get => _currentScale.Z;
            set => DoDirectTransform(_currentPos, _currentRot, new SimdVector3(_currentScale.X, _currentScale.Y, value));
        }

        private DispatcherTimer DelayedSelectedEventTimer { get; set; }

        public ModelSceneView(ModelScene modelScene)
        {
            DataContext = this;
            ModelScene = modelScene;

            //Delayed event timer for reacting to selected item changes. This is needed because the TreeView events fire off BEFORE the attached property (SelectedItems) does its thing, so we need another event to fire of after a small delay
            DelayedSelectedEventTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(50) };
            DelayedSelectedEventTimer.Tick += (s, e) =>
            {
                DelayedSelectedEventTimer.Stop();
                OnSelectedItemChanged();
            };
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            EmdFile?.RefreshValues();
            TextureViewModel?.UpdateProperties();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            DelayedSelectedEventTimer.Start();
        }

        private void OnSelectedItemChanged()
        {
            SortSelectedItems();

            if (SelectedTexture != null)
            {
                TextureViewModel = new EmdTextureViewModel(SelectedTexture, EmdFile.GetParentSubmesh(SelectedTexture), EmdFile, ModelScene.EmbFile);
                NotifyPropertyChanged(nameof(TextureViewModel));
            }

            NotifyPropertyChanged(nameof(ModelNameVisibility));
            NotifyPropertyChanged(nameof(MeshNameVisibility));
            NotifyPropertyChanged(nameof(SubmeshNameVisibility));
            NotifyPropertyChanged(nameof(TransformVisibility));
            NotifyPropertyChanged(nameof(TextureVisibility));
            NotifyPropertyChanged(nameof(SelectedModelName));
            NotifyPropertyChanged(nameof(SelectedMeshName));
            NotifyPropertyChanged(nameof(SelectedSubmeshName));
            TryEnableModelGizmo();
        }

        private void treeView_Selected(object sender, RoutedEventArgs e)
        {

            /*
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;

            if (tvi == null || e.Handled) return;

            if (!tvi.IsExpanded)
                tvi.IsExpanded = true;

            //tvi.IsExpanded = !tvi.IsExpanded;
            e.Handled = true;
            */
        }

        private void TryEnableModelGizmo()
        {
            if (Viewport.Instance != null)
            {
                Viewport.Instance.ModelGizmo.SetCallback(TransformOperationBegin, TransformOperationComplete);
                UpdateTransformValues();

                var submeshes = ModelScene.Model.GetAllSubmeshesFromSourceObject(SelectedItem);

                if(submeshes?.Count > 0)
                {
                    Viewport.Instance.ModelGizmo.SetContext(submeshes, GetAttachBone(submeshes));
                }
                else
                {
                    Viewport.Instance.ModelGizmo.RemoveContext();
                }
            }
        }

        private void TransformOperationBegin()
        {
            ModelScene.AlwaysUpdateBoundingBox = true;
        }

        private void TransformOperationComplete()
        {
            ModelScene.AlwaysUpdateBoundingBox = false;
            UpdateTransformValues();
        }

        private void UpdateTransformValues()
        {
            var submeshes = ModelScene.Model.GetAllSubmeshesFromSourceObject(SelectedItem);

            if(submeshes?.Count > 0 )
            {
                SimdVector3 centerPos = Xv2Submesh.CalculateCenter(submeshes);

                Matrix4x4 matrix = submeshes[0].Transform;

                if(ModelScene.Skeleton != null)
                {
                    var attachBone = GetAttachBone(submeshes);

                    if(attachBone != null)
                        matrix *= attachBone.AbsoluteAnimationMatrix;
                }

                if (Matrix4x4.Decompose(matrix, out SimdVector3 _scale, out SimdQuaternion _rot, out SimdVector3 _translation))
                {
                    _currentPos = _translation + centerPos;
                    _currentScale = _scale;
                    _currentRot = _rot.ToEuler();
                }

                NotifyPropertyChanged(nameof(PosX));
                NotifyPropertyChanged(nameof(PosY));
                NotifyPropertyChanged(nameof(PosZ));
                NotifyPropertyChanged(nameof(RotX));
                NotifyPropertyChanged(nameof(RotY));
                NotifyPropertyChanged(nameof(RotZ));
                NotifyPropertyChanged(nameof(ScaleX));
                NotifyPropertyChanged(nameof(ScaleY));
                NotifyPropertyChanged(nameof(ScaleZ));
                ModelScene.IsBoundingBoxDirty = true;
            }
        }

        private void DoDirectTransform(SimdVector3 newPos, SimdVector3 newRot, SimdVector3 newScale)
        {
            var submeshes = ModelScene.Model.GetAllSubmeshesFromSourceObject(SelectedItem);
            newPos -= Xv2Submesh.CalculateCenter(submeshes);

            if (submeshes?.Count > 0)
            {
                //Create a inverse matrix so we can strip the skeleton from the values if one exists (NSK/EMO)
                Matrix4x4 invSkeletonMarix = Matrix4x4.Identity;

                if (ModelScene.Skeleton != null)
                {
                    var attachBone = GetAttachBone(submeshes);

                    if (attachBone != null)
                    {
                        invSkeletonMarix = MathHelpers.Invert(attachBone.AbsoluteAnimationMatrix);
                    }
                }

                newRot.ClampEuler();
                newScale.ClampScale();

                Matrix4x4 deltaMatrix = Matrix4x4.CreateScale(newScale);
                deltaMatrix *= Matrix4x4.CreateFromQuaternion(newRot.EulerToQuaternion());
                deltaMatrix *= Matrix4x4.CreateTranslation(newPos);
                deltaMatrix *= MathHelpers.Invert(submeshes[0].Transform);
                deltaMatrix *= invSkeletonMarix;

                foreach(var submesh in submeshes)
                {
                    submesh.Transform *= deltaMatrix;
                }

                UpdateTransformValues();
            }
        }

        private Xv2Bone GetAttachBone(IList<Xv2Submesh> submeshes)
        {
            return submeshes[0].Parent.Parent.AttachBone;
        }

        #region Commands
        public RelayCommand DeleteModelCommand => new RelayCommand(ModelScene.DeleteSelectedModels, ModelScene.IsModelSelected);

        public RelayCommand DeleteMeshCommand => new RelayCommand(ModelScene.DeleteSelectedMeshes, ModelScene.IsMeshSelected);
        
        public RelayCommand DeleteSubmeshCommand => new RelayCommand(ModelScene.DeleteSelectedSubmeshes, ModelScene.IsSubmeshSelected);
        
        public RelayCommand DeleteTextureCommand => new RelayCommand(ModelScene.DeleteSelectedTextures, ModelScene.IsTextureSelected);

        public RelayCommand CopyModelCommand => new RelayCommand(ModelScene.CopySelectedModels, ModelScene.IsModelSelected);

        public RelayCommand CopyMeshCommand => new RelayCommand(ModelScene.CopySelectedMeshes, ModelScene.IsMeshSelected);
 
        public RelayCommand CopySubmeshCommand => new RelayCommand(ModelScene.CopySelectedSubmeshes, ModelScene.IsSubmeshSelected);

        public RelayCommand CopyTextureCommand => new RelayCommand(CopyTexture, ModelScene.IsTextureSelected);
        private void CopyTexture()
        {
            Clipboard.SetData(ClipboardConstants.EmdTextureSampler, SelectedTexture);
        }

        public RelayCommand PasteModelCommand => new RelayCommand(ModelScene.PasteModels, ModelScene.CanPasteModel);
        
        public RelayCommand PasteMeshCommand => new RelayCommand(ModelScene.PasteMeshes, ModelScene.CanPasteMesh);
        
        public RelayCommand PasteSubmeshCommand => new RelayCommand(ModelScene.PasteSubmeshes, ModelScene.CanPasteSubmesh);
        
        public RelayCommand PasteTextureCommand => new RelayCommand(PasteTexture, CanPasteTexture);
        private void PasteTexture()
        {
            EMD_Submesh submesh = SelectedSubmesh != null ? SelectedSubmesh : EmdFile.GetParentSubmesh(SelectedTexture);

            if (submesh.TextureSamplerDefs.Count >= 4)
            {
                MessageBox.Show("Cannot add anymore Texture Samplers.", "Maximum Texture Samplers", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();

            EMD_TextureSamplerDef texture = (EMD_TextureSamplerDef)Clipboard.GetData(ClipboardConstants.EmdTextureSampler);

            undos.Add(new UndoableListAdd<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, texture));
            submesh.TextureSamplerDefs.Add(texture);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, texture, submesh)));
            EmdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submesh);

            UndoManager.Instance.AddCompositeUndo(undos, "Paste Texture Sampler");
        }

        public RelayCommand AddTextureCommand => new RelayCommand(AddTexture, CanAddTexture);
        private void AddTexture()
        {
            EMD_Submesh submesh = SelectedSubmesh != null ? SelectedSubmesh : EmdFile.GetParentSubmesh(SelectedTexture);

            if (submesh.TextureSamplerDefs.Count >= 4)
            {
                MessageBox.Show("Cannot add anymore Texture Samplers.", "Maximum Texture Samplers", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();

            EMD_TextureSamplerDef texture = new EMD_TextureSamplerDef();

            undos.Add(new UndoableListAdd<EMD_TextureSamplerDef>(submesh.TextureSamplerDefs, texture));
            submesh.TextureSamplerDefs.Add(texture);

            undos.Add(new UndoActionDelegate(EmdFile, nameof(EmdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, texture, submesh)));
            EmdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submesh);

            UndoManager.Instance.AddCompositeUndo(undos, "Add Texture Sampler");
        }

        public RelayCommand GotoTextureCommand => new RelayCommand(GotoTexture, () => TextureViewModel?.SelectedEmbEntry != null);
        private void GotoTexture()
        {
            var window = WindowHelper.GetActiveEmbForm(ModelScene.EmbFile);

            if (window == null)
            {
                window = new EmbEditForm(ModelScene.EmbFile, TextureEditorType.Character, Path.GetFileName(ModelScene.EmbPath));
                window.Show();
            }
            else
            {
                window.Focus();
            }

            window.SelectTexture(ModelScene.EmbFile.GetEntry(SelectedTexture.EmbIndex));
        }

        public RelayCommand GotoMaterialCommand => new RelayCommand(GotoMaterial, CanGotoMaterial);
        private void GotoMaterial()
        {
            var window = WindowHelper.GetActiveEmmForm(ModelScene.EmmFile);

            if (window == null)
            {
                window = new MaterialsEditorForm(ModelScene.EmmFile, Path.GetFileName(ModelScene.EmmPath));
                window.Show();
            }
            else
            {
                window.Focus();
            }

            window.SelectMaterial(ModelScene.EmmFile.GetMaterial(SelectedSubmeshName));
        }

        public RelayCommand LookAtObjectCommand => new RelayCommand(LookAtObject, IsAnythingSelected);
        private void LookAtObject()
        {
            if(SelectedTexture == null)
            {
                Viewport.Instance.Camera.LookAt(ModelScene.BoundingBox);
                Log.Add("LookAt from View");
            }
        }


        private bool CanPasteTexture()
        {
            return Clipboard.ContainsData(ClipboardConstants.EmdTextureSampler) && (SelectedSubmesh != null || SelectedTexture != null);
        }

        private bool CanAddTexture()
        {
            return SelectedSubmesh != null || SelectedTexture != null;
        }
        
        private bool CanGotoMaterial()
        {
            //Checks if submesh is selected, and then if a material exists for it
            if (!ModelScene.IsSubmeshSelected()) return false;
            return ModelScene.EmmFile?.GetMaterial(SelectedSubmeshName) != null;
        }
        #endregion


        #region InputCommands
        public RelayCommand DeleteCommand => new RelayCommand(DeleteAnything, IsAnythingSelected);
        private void DeleteAnything()
        {
            if (!ModelScene.HasOnlyOneSelectedType()) return;

            if (ModelScene.IsModelSelected())
            {
                ModelScene.DeleteSelectedModels();
            }
            else if (ModelScene.IsMeshSelected())
            {
                ModelScene.DeleteSelectedMeshes();
            }
            else if (ModelScene.IsSubmeshSelected())
            {
                ModelScene.DeleteSelectedSubmeshes();
            }
            else if (ModelScene.IsTextureSelected())
            {
                ModelScene.DeleteSelectedTextures();
            }
        }

        public RelayCommand CopyCommand => new RelayCommand(CopyAnything, IsAnythingSelected);
        private void CopyAnything()
        {
            if (ModelScene.IsModelSelected())
            {
                ModelScene.CopySelectedModels();
            }
            else if (ModelScene.IsMeshSelected())
            {
                ModelScene.CopySelectedMeshes();
            }
            else if (ModelScene.IsSubmeshSelected())
            {
                ModelScene.CopySelectedSubmeshes();
            }
            else if (SelectedTexture != null)
            {
                CopyTexture();
            }
        }

        public RelayCommand PasteCommand => new RelayCommand(PasteAnything, CanPasteAnything);
        private void PasteAnything()
        {
            if (ModelScene.CanPasteModel())
            {
                ModelScene.PasteModels();
            }
            else if (ModelScene.CanPasteMesh())
            {
                ModelScene.PasteMeshes();
            }
            else if (ModelScene.CanPasteSubmesh())
            {
                ModelScene.PasteSubmeshes();
            }
            else if (CanPasteTexture())
            {
                PasteTexture();
            }
        }


        private bool CanPasteAnything()
        {
            return ModelScene.CanPasteModel() || ModelScene.CanPasteMesh() || ModelScene.CanPasteSubmesh() || CanPasteTexture();
        }

        private bool IsAnythingSelected()
        {
            return ModelScene.SelectedItems.Count > 0;
        }
        #endregion

        #region SelectedItemSorting
        private void SortSelectedItems()
        {
            //Sort selected items so it only contains one type of object (no mixing of model, mesh, texture etc)
            if (SelectedItems.Count > 0)
            {
                Type currentType = SelectedItems[SelectedItems.Count - 1].GetType();

                for (int i = SelectedItems.Count - 2; i >= 0; i--)
                {
                    if (SelectedItems[i].GetType() != currentType)
                    {
                        SelectedItems.RemoveAt(i);
                    }
                }
            }

            RefreshSelectedItems(treeView);
        }

        private static void RefreshSelectedItems(TreeView treeView)
        {
            IList selectedItems = LB_Common.Utils.TreeViewMultipleSelectionAttached.GetSelectedItems(treeView);
            RefreshSelectedItems(selectedItems, treeView.Items, treeView.ItemContainerGenerator);
        }

        private static void RefreshSelectedItems(IList selectedItems, IEnumerable items, ItemContainerGenerator treeView)
        {
            foreach (var item in items)
            {
                TreeViewItem tvi = treeView.ContainerFromItem(item) as TreeViewItem;

                if (tvi != null)
                {
                    LB_Common.Utils.TreeViewMultipleSelectionAttached.SetIsItemSelected(tvi, selectedItems.Contains(item));

                    if (item is EMD_Model emdModel)
                    {
                        RefreshSelectedItems(selectedItems, emdModel.Meshes, tvi.ItemContainerGenerator);
                    }
                    else if (item is EMD_Mesh emdMesh)
                    {
                        RefreshSelectedItems(selectedItems, emdMesh.Submeshes, tvi.ItemContainerGenerator);
                    }
                    else if (item is EMD_Submesh emdSubmesh)
                    {
                        RefreshSelectedItems(selectedItems, emdSubmesh.TextureSamplerDefs, tvi.ItemContainerGenerator);
                    }
                }


            }
        }
        #endregion
    }
}