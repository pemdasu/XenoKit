using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BCS
{
    public class BcsSkeletonDataViewModel : ObservableObject
    {
        private SkeletonData skeletonData;
        private Bone bone;

        public string BoneName
        {
            get
            {
                return bone.BoneName;
            }
            set
            {
                if (Equals(bone.BoneName, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.BoneName), bone, bone.BoneName, value, "SkeletonData BoneName"), UndoGroup.BCS, undoContext: skeletonData);
                bone.BoneName = value;
                RaisePropertyChanged(() => BoneName);
                bone.RefreshValues();
            }
        }
        public int I_00
        {
            get
            {
                return bone.I_00;
            }
            set
            {
                if (Equals(bone.I_00, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.I_00), bone, bone.I_00, value, "SkeletonData I_00"), UndoGroup.BCS, undoContext: skeletonData);
                bone.I_00 = value;
                RaisePropertyChanged(() => I_00);
            }
        }
        public int I_04
        {
            get
            {
                return bone.I_04;
            }
            set
            {
                if (Equals(bone.I_04, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.I_04), bone, bone.I_04, value, "SkeletonData I_04"), UndoGroup.BCS, undoContext: skeletonData);
                bone.I_04 = value;
                RaisePropertyChanged(() => I_04);
                RaisePropertyChanged(nameof(Shape));
                RaisePropertyChanged(nameof(ShapeLabel));
            }
        }
        public float F_12
        {
            get
            {
                return bone.F_12;
            }
            set
            {
                if (Equals(bone.F_12, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_12), bone, bone.F_12, value, "SkeletonData F_12"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_12 = value;
                RaisePropertyChanged(() => F_12);
            }
        }
        public float F_16
        {
            get
            {
                return bone.F_16;
            }
            set
            {
                if (Equals(bone.F_16, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_16), bone, bone.F_16, value, "SkeletonData F_16"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_16 = value;
                RaisePropertyChanged(() => F_16);
            }
        }
        public float F_20
        {
            get
            {
                return bone.F_20;
            }
            set
            {
                if (Equals(bone.F_20, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_20), bone, bone.F_20, value, "SkeletonData F_20"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_20 = value;
                RaisePropertyChanged(() => F_20);
            }
        }
        public float F_24
        {
            get
            {
                return bone.F_24;
            }
            set
            {
                if (Equals(bone.F_24, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_24), bone, bone.F_24, value, "SkeletonData F_24"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_24 = value;
                RaisePropertyChanged(() => F_24);
            }
        }
        public float F_28
        {
            get
            {
                return bone.F_28;
            }
            set
            {
                if (Equals(bone.F_28, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_28), bone, bone.F_28, value, "SkeletonData F_28"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_28 = value;
                RaisePropertyChanged(() => F_28);
            }
        }
        public float F_32
        {
            get
            {
                return bone.F_32;
            }
            set
            {
                if (Equals(bone.F_32, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_32), bone, bone.F_32, value, "SkeletonData F_32"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_32 = value;
                RaisePropertyChanged(() => F_32);
            }
        }
        public float F_36
        {
            get
            {
                return bone.F_36;
            }
            set
            {
                if (Equals(bone.F_36, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_36), bone, bone.F_36, value, "SkeletonData F_36"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_36 = value;
                RaisePropertyChanged(() => F_36);
            }
        }
        public float F_40
        {
            get
            {
                return bone.F_40;
            }
            set
            {
                if (Equals(bone.F_40, value)) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_40), bone, bone.F_40, value, "SkeletonData F_40"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_40 = value;
                RaisePropertyChanged(() => F_40);
            }
        }

        public IReadOnlyDictionary<int, string> Shapes { get; } = new Dictionary<int, string>
        {
            { 0, "Box" }, { 1, "Sphere" }, { 2, "Cylinder" }, { 3, "Capsule" }
        };

        public int Shape
        {
            get => bone.I_04 & 0xffff;
            set
            {
                if (Shape == value) return;
                I_04 = (bone.I_04 & unchecked((int)0xffff0000)) | (value & 0xffff);
            }
        }

        public string ShapeLabel => Shapes.ContainsKey(Shape) ? Shapes[Shape] : $"Unknown ({Shape})";

        public float F_44
        {
            get => bone.F_44;
            set
            {
                if (bone.F_44 == value) return;
                UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(bone.F_44), bone, bone.F_44, value, "Collider depth"), UndoGroup.BCS, undoContext: skeletonData);
                bone.F_44 = value;
                RaisePropertyChanged(nameof(F_44));
            }
        }

        public BcsSkeletonDataViewModel(Bone bone, SkeletonData skeletonData)
        {
            this.bone = bone;
            this.skeletonData = skeletonData;
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(nameof(BoneName));
            RaisePropertyChanged(nameof(I_00));
            RaisePropertyChanged(nameof(I_04));
            RaisePropertyChanged(nameof(F_12));
            RaisePropertyChanged(nameof(F_16));
            RaisePropertyChanged(nameof(F_20));
            RaisePropertyChanged(nameof(F_24));
            RaisePropertyChanged(nameof(F_28));
            RaisePropertyChanged(nameof(F_32));
            RaisePropertyChanged(nameof(F_36));
            RaisePropertyChanged(nameof(F_40));
            RaisePropertyChanged(nameof(F_44));
            RaisePropertyChanged(nameof(Shape));
            RaisePropertyChanged(nameof(ShapeLabel));
        }
        
        public bool IsBone(Bone bone)
        {
            return this.bone == bone;
        }
    }
}
