using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType4ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type4 type;

        public int BoundsType { get => type.I_00; set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Deflection Bounds Type"); }
        public float PositionX { get => type.F_08; set => SetValue(nameof(type.F_08), type.F_08, value, v => type.F_08 = v, "BSA Deflection Position X"); }
        public float PositionY { get => type.F_12; set => SetValue(nameof(type.F_12), type.F_12, value, v => type.F_12 = v, "BSA Deflection Position Y"); }
        public float PositionZ { get => type.F_16; set => SetValue(nameof(type.F_16), type.F_16, value, v => type.F_16 = v, "BSA Deflection Position Z"); }
        public float BoundsSize { get => type.F_20; set => SetValue(nameof(type.F_20), type.F_20, value, v => type.F_20 = v, "BSA Deflection Bounds Size"); }
        public float VectorAX { get => type.F_24; set => SetValue(nameof(type.F_24), type.F_24, value, v => type.F_24 = v, "BSA Deflection Vector A X"); }
        public float VectorAY { get => type.F_28; set => SetValue(nameof(type.F_28), type.F_28, value, v => type.F_28 = v, "BSA Deflection Vector A Y"); }
        public float VectorAZ { get => type.F_32; set => SetValue(nameof(type.F_32), type.F_32, value, v => type.F_32 = v, "BSA Deflection Vector A Z"); }
        public float VectorBX { get => type.F_36; set => SetValue(nameof(type.F_36), type.F_36, value, v => type.F_36 = v, "BSA Deflection Vector B X"); }
        public float VectorBY { get => type.F_40; set => SetValue(nameof(type.F_40), type.F_40, value, v => type.F_40 = v, "BSA Deflection Vector B Y"); }
        public float VectorBZ { get => type.F_44; set => SetValue(nameof(type.F_44), type.F_44, value, v => type.F_44 = v, "BSA Deflection Vector B Z"); }
        public ushort Priority { get => type.I_50; set => SetValue(nameof(type.I_50), type.I_50, value, v => type.I_50 = v, "BSA Deflection Priority"); }
        public ushort BdmId { get => type.I_52; set => SetValue(nameof(type.I_52), type.I_52, value, v => type.I_52 = v, "BSA Deflection BDM ID"); }
        public ushort I_48 { get => type.I_48; set => SetValue(nameof(type.I_48), type.I_48, value, v => type.I_48 = v, "BSA Deflection I_48"); }
        public ushort I_54 { get => type.I_54; set => SetValue(nameof(type.I_54), type.I_54, value, v => type.I_54 = v, "BSA Deflection I_54"); }

        public BsaType4ViewModel(BSA_Type4 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => BoundsType);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => BoundsSize);
            RaisePropertyChanged(() => VectorAX);
            RaisePropertyChanged(() => VectorAY);
            RaisePropertyChanged(() => VectorAZ);
            RaisePropertyChanged(() => VectorBX);
            RaisePropertyChanged(() => VectorBY);
            RaisePropertyChanged(() => VectorBZ);
            RaisePropertyChanged(() => Priority);
            RaisePropertyChanged(() => BdmId);
            RaisePropertyChanged(() => I_48);
            RaisePropertyChanged(() => I_54);
        }
    }
}
