using System.Windows;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType14ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type14 type;

        public SpatialEffectGeometryMode PlacementMode { get => type.I_00; set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Type14 Placement Mode"); }
        public ushort I_02 { get => type.I_02; set => SetValue(nameof(type.I_02), type.I_02, value, v => type.I_02 = v, "BSA Type14 I_02"); }
        private uint PlacementFlags
        {
            get => type.F_04;
            set
            {
                SetValue(nameof(type.F_04), type.F_04, value, v => type.F_04 = v, "BSA Type14 Placement Flags");
                RaisePropertyChanged(() => DistanceBasedPlacement);
                RaisePropertyChanged(() => UseActiveObjectPosition);
            }
        }
        public bool DistanceBasedPlacement { get => (PlacementFlags & 0x1) != 0; set => SetPlacementFlag(0x1, value); }
        public bool UseActiveObjectPosition { get => (PlacementFlags & 0x4) != 0; set => SetPlacementFlag(0x4, value); }
        public uint I_08 { get => type.I_08; set => SetValue(nameof(type.I_08), type.I_08, value, v => type.I_08 = v, "BSA Type14 I_08"); }
        public float F_12 { get => type.F_12; set => SetValue(nameof(type.F_12), type.F_12, value, v => type.F_12 = v, "BSA Type14 F_12"); }
        public uint I_16 { get => type.I_16; set => SetValue(nameof(type.I_16), type.I_16, value, v => type.I_16 = v, "BSA Type14 I_16"); }
        public float F_20 { get => type.F_20; set => SetValue(nameof(type.F_20), type.F_20, value, v => type.F_20 = v, "BSA Type14 F_20"); }
        public uint I_24 { get => type.I_24; set => SetValue(nameof(type.I_24), type.I_24, value, v => type.I_24 = v, "BSA Type14 I_24"); }
        public float F_28 { get => type.F_28; set => SetValue(nameof(type.F_28), type.F_28, value, v => type.F_28 = v, "BSA Type14 F_28"); }
        public uint I_32 { get => type.I_32; set => SetValue(nameof(type.I_32), type.I_32, value, v => type.I_32 = v, "BSA Type14 I_32"); }
        public uint I_36 { get => type.I_36; set => SetValue(nameof(type.I_36), type.I_36, value, v => type.I_36 = v, "BSA Type14 I_36"); }
        public uint I_40 { get => type.I_40; set => SetValue(nameof(type.I_40), type.I_40, value, v => type.I_40 = v, "BSA Type14 I_40"); }
        public float F_44 { get => type.F_44; set => SetValue(nameof(type.F_44), type.F_44, value, v => type.F_44 = v, "BSA Type14 F_44"); }
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
        public uint SkillID { get => type.F_52; set => SetValue(nameof(type.F_52), type.F_52, value, v => type.F_52 = v, "BSA Type14 Skill ID"); }
        public uint EffectID { get => type.I_56; set => SetValue(nameof(type.I_56), type.I_56, value, v => type.I_56 = v, "BSA Type14 Effect ID"); }
        public float PositionX { get => type.F_60; set => SetValue(nameof(type.F_60), type.F_60, value, v => type.F_60 = v, "BSA Effect Placement Position X"); }
        public float PositionY { get => type.PlacementPositionY; set => SetValue(nameof(type.PlacementPositionY), type.PlacementPositionY, value, v => type.PlacementPositionY = v, "BSA Effect Placement Position Y"); }
        public float PositionZ { get => type.F_68; set => SetValue(nameof(type.F_68), type.F_68, value, v => type.F_68 = v, "BSA Effect Placement Position Z"); }
        public float RotationX { get => type.PlacementRotationX; set => SetValue(nameof(type.PlacementRotationX), type.PlacementRotationX, value, v => type.PlacementRotationX = v, "BSA Effect Placement Rotation X"); }
        public float RotationY { get => type.PlacementRotationY; set => SetValue(nameof(type.PlacementRotationY), type.PlacementRotationY, value, v => type.PlacementRotationY = v, "BSA Effect Placement Rotation Y"); }
        public float RotationZ { get => type.PlacementRotationZ; set => SetValue(nameof(type.PlacementRotationZ), type.PlacementRotationZ, value, v => type.PlacementRotationZ = v, "BSA Effect Placement Rotation Z"); }
        private uint EffectPlacementFlags
        {
            get => type.I_84;
            set
            {
                SetValue(nameof(type.I_84), type.I_84, value, v => type.I_84 = v, "BSA Type14 Effect Placement Flags");
                RaisePropertyChanged(() => NormalizePlacementDirection);
                RaisePropertyChanged(() => UseExplicitDirectionA);
                RaisePropertyChanged(() => UseExplicitDirectionB);
                RaisePropertyChanged(() => UseAlternatePlacementPath);
            }
        }
        public bool NormalizePlacementDirection { get => (EffectPlacementFlags & 0x2) != 0; set => SetEffectPlacementFlag(0x2, value); }
        public bool UseExplicitDirectionA { get => (EffectPlacementFlags & 0x10) != 0; set => SetEffectPlacementFlag(0x10, value); }
        public bool UseExplicitDirectionB { get => (EffectPlacementFlags & 0x20) != 0; set => SetEffectPlacementFlag(0x20, value); }
        public bool UseAlternatePlacementPath { get => (EffectPlacementFlags & 0x40) != 0; set => SetEffectPlacementFlag(0x40, value); }

        public BsaType14ViewModel(BSA_Type14 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => PlacementMode);
            RaisePropertyChanged(() => I_02);
            RaisePropertyChanged(() => DistanceBasedPlacement);
            RaisePropertyChanged(() => UseActiveObjectPosition);
            RaisePropertyChanged(() => I_08);
            RaisePropertyChanged(() => F_12);
            RaisePropertyChanged(() => I_16);
            RaisePropertyChanged(() => F_20);
            RaisePropertyChanged(() => I_24);
            RaisePropertyChanged(() => F_28);
            RaisePropertyChanged(() => I_32);
            RaisePropertyChanged(() => I_36);
            RaisePropertyChanged(() => I_40);
            RaisePropertyChanged(() => F_44);
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
            RaisePropertyChanged(() => NormalizePlacementDirection);
            RaisePropertyChanged(() => UseExplicitDirectionA);
            RaisePropertyChanged(() => UseExplicitDirectionB);
            RaisePropertyChanged(() => UseAlternatePlacementPath);
        }

        private void SetPlacementFlag(uint flag, bool value)
        {
            PlacementFlags = value ? PlacementFlags | flag : PlacementFlags & ~flag;
        }

        private void SetEffectPlacementFlag(uint flag, bool value)
        {
            EffectPlacementFlags = value ? EffectPlacementFlags | flag : EffectPlacementFlags & ~flag;
        }
    }
}
