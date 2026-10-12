using System.Windows;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType14ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type14 type;

        public BsaShapeType ShapeType
        {
            get => type.ShapeType;
            set
            {
                SetValue(nameof(type.ShapeType), type.ShapeType, value, v => type.ShapeType = v, "BSA Target Effect Volume Shape");
                RaiseShapeProperties();
            }
        }

        public string SizeLabel => type.ShapeType == BsaShapeType.Box ? "Size X" : "Radius";
        public Visibility BoxVisibility => type.ShapeType == BsaShapeType.Box ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CapsuleVisibility => type.ShapeType == BsaShapeType.Capsule ? Visibility.Visible : Visibility.Collapsed;

        public ushort I_02 { get => type.I_02; set => SetValue(nameof(type.I_02), type.I_02, value, v => type.I_02 = v, "BSA Type14 I_02"); }

        private uint ShapeFlags
        {
            get => type.ShapeFlags;
            set
            {
                SetValue(nameof(type.ShapeFlags), type.ShapeFlags, value, v => type.ShapeFlags = v, "BSA Target Effect Volume Shape Flags");
                RaisePropertyChanged(() => StretchToSpawnPoint);
                RaisePropertyChanged(() => UseWorldAxes);
            }
        }

        public bool StretchToSpawnPoint { get => (ShapeFlags & 0x1) != 0; set => SetShapeFlag(0x1, value); }
        public bool UseWorldAxes { get => (ShapeFlags & 0x4) != 0; set => SetShapeFlag(0x4, value); }

        public float OffsetX { get => type.OffsetX; set => SetValue(nameof(type.OffsetX), type.OffsetX, value, v => type.OffsetX = v, "BSA Target Effect Volume Offset X"); }
        public float OffsetY { get => type.OffsetY; set => SetValue(nameof(type.OffsetY), type.OffsetY, value, v => type.OffsetY = v, "BSA Target Effect Volume Offset Y"); }
        public float OffsetZ { get => type.OffsetZ; set => SetValue(nameof(type.OffsetZ), type.OffsetZ, value, v => type.OffsetZ = v, "BSA Target Effect Volume Offset Z"); }
        public float Size { get => type.Radius; set => SetValue(nameof(type.Radius), type.Radius, value, v => type.Radius = v, "BSA Target Effect Volume Size"); }

        public float BoxSizeY { get => type.PointAX; set => SetShapeValue(nameof(type.PointAX), type.PointAX, value, v => type.PointAX = v, "BSA Target Effect Volume Box Size Y"); }
        public float BoxSizeZ { get => type.PointAY; set => SetShapeValue(nameof(type.PointAY), type.PointAY, value, v => type.PointAY = v, "BSA Target Effect Volume Box Size Z"); }
        public float BoxRotation1 { get => type.PointAZ; set => SetShapeValue(nameof(type.PointAZ), type.PointAZ, value, v => type.PointAZ = v, "BSA Target Effect Volume Box Rotation 1"); }
        public float BoxRotation2 { get => type.PointBX; set => SetShapeValue(nameof(type.PointBX), type.PointBX, value, v => type.PointBX = v, "BSA Target Effect Volume Box Rotation 2"); }
        public float BoxRotation3 { get => type.PointBY; set => SetShapeValue(nameof(type.PointBY), type.PointBY, value, v => type.PointBY = v, "BSA Target Effect Volume Box Rotation 3"); }

        public float PointAX { get => type.PointAX; set => SetShapeValue(nameof(type.PointAX), type.PointAX, value, v => type.PointAX = v, "BSA Target Effect Volume Point A X"); }
        public float PointAY { get => type.PointAY; set => SetShapeValue(nameof(type.PointAY), type.PointAY, value, v => type.PointAY = v, "BSA Target Effect Volume Point A Y"); }
        public float PointAZ { get => type.PointAZ; set => SetShapeValue(nameof(type.PointAZ), type.PointAZ, value, v => type.PointAZ = v, "BSA Target Effect Volume Point A Z"); }
        public float PointBX { get => type.PointBX; set => SetShapeValue(nameof(type.PointBX), type.PointBX, value, v => type.PointBX = v, "BSA Target Effect Volume Point B X"); }
        public float PointBY { get => type.PointBY; set => SetShapeValue(nameof(type.PointBY), type.PointBY, value, v => type.PointBY = v, "BSA Target Effect Volume Point B Y"); }
        public float PointBZ { get => type.PointBZ; set => SetShapeValue(nameof(type.PointBZ), type.PointBZ, value, v => type.PointBZ = v, "BSA Target Effect Volume Point B Z"); }

        public EepkType EepkType
        {
            get => type.EepkType;
            set
            {
                uint newValue = (type.I_48 & 0xFFFF0000u) | (uint)value;
                SetValue(nameof(type.I_48), type.I_48, newValue, v => type.I_48 = v, "BSA Type14 EEPK Type");
                RaisePropertyChanged(() => SkillIdVisibility);
                RaisePropertyChanged(() => CommonEepkVisibility);
            }
        }

        public Visibility SkillIdVisibility => type.EepkType == Xv2CoreLib.BSA.EepkType.Common ? Visibility.Collapsed : Visibility.Visible;
        public Visibility CommonEepkVisibility => type.EepkType == Xv2CoreLib.BSA.EepkType.Common ? Visibility.Visible : Visibility.Collapsed;

        public ushort TransformSelector
        {
            get => type.TransformSelector;
            set
            {
                uint newValue = (type.I_48 & 0x0000FFFFu) | ((uint)value << 16);
                SetValue(nameof(type.I_48), type.I_48, newValue, v => type.I_48 = v, "BSA Type14 Transform Selector");
            }
        }
        public uint SkillID { get => type.SkillID; set => SetValue(nameof(type.SkillID), type.SkillID, value, v => type.SkillID = v, "BSA Type14 Skill ID"); }
        public uint EffectID { get => type.EffectID; set => SetValue(nameof(type.EffectID), type.EffectID, value, v => type.EffectID = v, "BSA Type14 Effect ID"); }
        public float PositionX { get => type.F_60; set => SetValue(nameof(type.F_60), type.F_60, value, v => type.F_60 = v, "BSA Target Effect Position X"); }
        public float PositionY { get => type.TargetEffectPositionY; set => SetValue(nameof(type.TargetEffectPositionY), type.TargetEffectPositionY, value, v => type.TargetEffectPositionY = v, "BSA Target Effect Position Y"); }
        public float PositionZ { get => type.F_68; set => SetValue(nameof(type.F_68), type.F_68, value, v => type.F_68 = v, "BSA Target Effect Position Z"); }
        public float RotationX { get => type.TargetEffectRotationX; set => SetValue(nameof(type.TargetEffectRotationX), type.TargetEffectRotationX, value, v => type.TargetEffectRotationX = v, "BSA Target Effect Rotation X"); }
        public float RotationY { get => type.TargetEffectRotationY; set => SetValue(nameof(type.TargetEffectRotationY), type.TargetEffectRotationY, value, v => type.TargetEffectRotationY = v, "BSA Target Effect Rotation Y"); }
        public float RotationZ { get => type.TargetEffectRotationZ; set => SetValue(nameof(type.TargetEffectRotationZ), type.TargetEffectRotationZ, value, v => type.TargetEffectRotationZ = v, "BSA Target Effect Rotation Z"); }
        private uint TargetEffectFlags
        {
            get => type.TargetEffectFlags;
            set
            {
                SetValue(nameof(type.TargetEffectFlags), type.TargetEffectFlags, value, v => type.TargetEffectFlags = v, "BSA Target Effect Flags");
                RaisePropertyChanged(() => NormalizeDirection);
                RaisePropertyChanged(() => UseWorldPosition);
                RaisePropertyChanged(() => UseDirectionVector);
                RaisePropertyChanged(() => RefreshExistingEffect);
            }
        }
        public bool NormalizeDirection { get => (TargetEffectFlags & 0x2) != 0; set => SetTargetEffectFlag(0x2, value); }
        public bool UseWorldPosition { get => (TargetEffectFlags & 0x10) != 0; set => SetTargetEffectFlag(0x10, value); }
        public bool UseDirectionVector { get => (TargetEffectFlags & 0x20) != 0; set => SetTargetEffectFlag(0x20, value); }
        public bool RefreshExistingEffect { get => (TargetEffectFlags & 0x40) != 0; set => SetTargetEffectFlag(0x40, value); }

        public BsaType14ViewModel(BSA_Type14 type) : base(type)
        {
            this.type = type;
        }

        private void RaiseShapeProperties()
        {
            RaisePropertyChanged(() => ShapeType);
            RaisePropertyChanged(() => SizeLabel);
            RaisePropertyChanged(() => BoxVisibility);
            RaisePropertyChanged(() => CapsuleVisibility);
        }

        // Box size, box rotation and capsule points use the same model fields, so both sets update together.
        private void SetShapeValue(string modelProperty, float oldValue, float newValue, System.Action<float> assign, string undoName, [System.Runtime.CompilerServices.CallerMemberName] string viewModelProperty = null)
        {
            SetValue(modelProperty, oldValue, newValue, assign, undoName, viewModelProperty);
            RaiseShapeValues();
        }

        private void RaiseShapeValues()
        {
            RaisePropertyChanged(() => BoxSizeY);
            RaisePropertyChanged(() => BoxSizeZ);
            RaisePropertyChanged(() => BoxRotation1);
            RaisePropertyChanged(() => BoxRotation2);
            RaisePropertyChanged(() => BoxRotation3);
            RaisePropertyChanged(() => PointAX);
            RaisePropertyChanged(() => PointAY);
            RaisePropertyChanged(() => PointAZ);
            RaisePropertyChanged(() => PointBX);
            RaisePropertyChanged(() => PointBY);
            RaisePropertyChanged(() => PointBZ);
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaiseShapeProperties();
            RaiseShapeValues();
            RaisePropertyChanged(() => I_02);
            RaisePropertyChanged(() => StretchToSpawnPoint);
            RaisePropertyChanged(() => UseWorldAxes);
            RaisePropertyChanged(() => OffsetX);
            RaisePropertyChanged(() => OffsetY);
            RaisePropertyChanged(() => OffsetZ);
            RaisePropertyChanged(() => Size);
            RaisePropertyChanged(() => EepkType);
            RaisePropertyChanged(() => SkillIdVisibility);
            RaisePropertyChanged(() => CommonEepkVisibility);
            RaisePropertyChanged(() => TransformSelector);
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => EffectID);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => RotationX);
            RaisePropertyChanged(() => RotationY);
            RaisePropertyChanged(() => RotationZ);
            RaisePropertyChanged(() => NormalizeDirection);
            RaisePropertyChanged(() => UseWorldPosition);
            RaisePropertyChanged(() => UseDirectionVector);
            RaisePropertyChanged(() => RefreshExistingEffect);
        }

        private void SetShapeFlag(uint flag, bool value)
        {
            ShapeFlags = value ? ShapeFlags | flag : ShapeFlags & ~flag;
        }

        private void SetTargetEffectFlag(uint flag, bool value)
        {
            TargetEffectFlags = value ? TargetEffectFlags | flag : TargetEffectFlags & ~flag;
        }
    }
}
