using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType8ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type8 type;

        public ushort BpeId
        {
            get => type.I_00;
            set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Post Effect ID");
        }

        public ushort AttachmentSelector
        {
            get => type.I_02;
            set => SetValue(nameof(type.I_02), type.I_02, value, v => type.I_02 = v, "BSA Post Effect Attachment");
        }

        public float PositionX
        {
            get => type.F_12;
            set => SetValue(nameof(type.F_12), type.F_12, value, v => type.F_12 = v, "BSA Post Effect Position X");
        }

        public float PositionY
        {
            get => type.F_16;
            set => SetValue(nameof(type.F_16), type.F_16, value, v => type.F_16 = v, "BSA Post Effect Position Y");
        }

        public float PositionZ
        {
            get => type.F_20;
            set => SetValue(nameof(type.F_20), type.F_20, value, v => type.F_20 = v, "BSA Post Effect Position Z");
        }

        public int I_04
        {
            get => type.I_04;
            set => SetValue(nameof(type.I_04), type.I_04, value, v => type.I_04 = v, "BSA Post Effect I_04");
        }

        public int I_08
        {
            get => type.I_08;
            set => SetValue(nameof(type.I_08), type.I_08, value, v => type.I_08 = v, "BSA Post Effect I_08");
        }

        public BsaType8ViewModel(BSA_Type8 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => BpeId);
            RaisePropertyChanged(() => AttachmentSelector);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => I_04);
            RaisePropertyChanged(() => I_08);
        }
    }
}
