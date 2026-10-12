using System.Windows;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType4ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type4 type;

        public BsaShapeType ShapeType
        {
            get => type.ShapeType;
            set
            {
                SetValue(nameof(type.ShapeType), type.ShapeType, value, v => type.ShapeType = v, "BSA Deflection Shape");
                RaiseShapeProperties();
            }
        }

        public string SizeLabel => type.ShapeType == BsaShapeType.Box ? "Size X" : "Radius";
        public Visibility BoxVisibility => type.ShapeType == BsaShapeType.Box ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CapsuleVisibility => type.ShapeType == BsaShapeType.Capsule ? Visibility.Visible : Visibility.Collapsed;

        private int ShapeFlags
        {
            get => type.ShapeFlags;
            set
            {
                SetValue(nameof(type.ShapeFlags), type.ShapeFlags, value, v => type.ShapeFlags = v, "BSA Deflection Shape Flags");
                RaisePropertyChanged(() => StretchToSpawnPoint);
                RaisePropertyChanged(() => UseWorldAxes);
            }
        }

        public bool StretchToSpawnPoint { get => (ShapeFlags & 0x1) != 0; set => SetShapeFlag(0x1, value); }
        public bool UseWorldAxes { get => (ShapeFlags & 0x4) != 0; set => SetShapeFlag(0x4, value); }

        public float PositionX { get => type.F_08; set => SetValue(nameof(type.F_08), type.F_08, value, v => type.F_08 = v, "BSA Deflection Position X"); }
        public float PositionY { get => type.F_12; set => SetValue(nameof(type.F_12), type.F_12, value, v => type.F_12 = v, "BSA Deflection Position Y"); }
        public float PositionZ { get => type.F_16; set => SetValue(nameof(type.F_16), type.F_16, value, v => type.F_16 = v, "BSA Deflection Position Z"); }
        public float BoundsSize { get => type.F_20; set => SetValue(nameof(type.F_20), type.F_20, value, v => type.F_20 = v, "BSA Deflection Size"); }

        public float BoxSizeY { get => type.F_24; set => SetShapeValue(nameof(type.F_24), type.F_24, value, v => type.F_24 = v, "BSA Deflection Box Size Y"); }
        public float BoxSizeZ { get => type.F_28; set => SetShapeValue(nameof(type.F_28), type.F_28, value, v => type.F_28 = v, "BSA Deflection Box Size Z"); }
        public float BoxRotation1 { get => type.F_32; set => SetShapeValue(nameof(type.F_32), type.F_32, value, v => type.F_32 = v, "BSA Deflection Box Rotation 1"); }
        public float BoxRotation2 { get => type.F_36; set => SetShapeValue(nameof(type.F_36), type.F_36, value, v => type.F_36 = v, "BSA Deflection Box Rotation 2"); }
        public float BoxRotation3 { get => type.F_40; set => SetShapeValue(nameof(type.F_40), type.F_40, value, v => type.F_40 = v, "BSA Deflection Box Rotation 3"); }

        public float PointAX { get => type.F_24; set => SetShapeValue(nameof(type.F_24), type.F_24, value, v => type.F_24 = v, "BSA Deflection Point A X"); }
        public float PointAY { get => type.F_28; set => SetShapeValue(nameof(type.F_28), type.F_28, value, v => type.F_28 = v, "BSA Deflection Point A Y"); }
        public float PointAZ { get => type.F_32; set => SetShapeValue(nameof(type.F_32), type.F_32, value, v => type.F_32 = v, "BSA Deflection Point A Z"); }
        public float PointBX { get => type.F_36; set => SetShapeValue(nameof(type.F_36), type.F_36, value, v => type.F_36 = v, "BSA Deflection Point B X"); }
        public float PointBY { get => type.F_40; set => SetShapeValue(nameof(type.F_40), type.F_40, value, v => type.F_40 = v, "BSA Deflection Point B Y"); }
        public float PointBZ { get => type.F_44; set => SetShapeValue(nameof(type.F_44), type.F_44, value, v => type.F_44 = v, "BSA Deflection Point B Z"); }

        public ushort Priority { get => type.I_50; set => SetValue(nameof(type.I_50), type.I_50, value, v => type.I_50 = v, "BSA Deflection Priority"); }
        public ushort BdmId { get => type.I_52; set => SetValue(nameof(type.I_52), type.I_52, value, v => type.I_52 = v, "BSA Deflection BDM ID"); }
        public ushort I_48 { get => type.I_48; set => SetValue(nameof(type.I_48), type.I_48, value, v => type.I_48 = v, "BSA Deflection I_48"); }
        public ushort I_54 { get => type.I_54; set => SetValue(nameof(type.I_54), type.I_54, value, v => type.I_54 = v, "BSA Deflection I_54"); }

        public BsaType4ViewModel(BSA_Type4 type) : base(type)
        {
            this.type = type;
        }

        private void SetShapeFlag(int mask, bool value)
        {
            ShapeFlags = value ? ShapeFlags | mask : ShapeFlags & ~mask;
        }

        // Box size, box rotation and capsule points use the same model fields, so both sets update together.
        private void SetShapeValue(string modelProperty, float oldValue, float newValue, System.Action<float> assign, string undoName, [System.Runtime.CompilerServices.CallerMemberName] string viewModelProperty = null)
        {
            SetValue(modelProperty, oldValue, newValue, assign, undoName, viewModelProperty);
            RaiseShapeValues();
        }

        private void RaiseShapeProperties()
        {
            RaisePropertyChanged(() => ShapeType);
            RaisePropertyChanged(() => SizeLabel);
            RaisePropertyChanged(() => BoxVisibility);
            RaisePropertyChanged(() => CapsuleVisibility);
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
            RaisePropertyChanged(() => StretchToSpawnPoint);
            RaisePropertyChanged(() => UseWorldAxes);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => BoundsSize);
            RaisePropertyChanged(() => Priority);
            RaisePropertyChanged(() => BdmId);
            RaisePropertyChanged(() => I_48);
            RaisePropertyChanged(() => I_54);
        }
    }
}
