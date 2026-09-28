using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType6ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type6 effect;

        public EepkType EepkType
        {
            get => effect.EepkType;
            set => SetValue(nameof(effect.EepkType), effect.EepkType, value, v => effect.EepkType = v, "BSA Effect EEPK Type");
        }

        public ushort SkillID
        {
            get => effect.SkillID;
            set => SetValue(nameof(effect.SkillID), effect.SkillID, value, v => effect.SkillID = v, "BSA Effect Skill ID");
        }

        public ushort EffectID
        {
            get => effect.EffectID;
            set => SetValue(nameof(effect.EffectID), effect.EffectID, value, v => effect.EffectID = v, "BSA Effect ID");
        }

        public Switch Switch
        {
            get => (Switch)((ushort)effect.I_08 & 1);
            set => EffectFlags = (ushort)(((ushort)effect.I_08 & ~1) | ((ushort)value & 1));
        }

        private ushort EffectFlags
        {
            get => (ushort)effect.I_08;
            set
            {
                SetValue(nameof(effect.I_08), effect.I_08, (Switch)value, v => effect.I_08 = v, "BSA Effect Flags");
                RaisePropertyChanged(() => Switch);
                RaisePropertyChanged(() => UseNestedEffectPosition);
            }
        }

        public bool UseNestedEffectPosition
        {
            get => (EffectFlags & 0x100) != 0;
            set => EffectFlags = value ? (ushort)(EffectFlags | 0x100) : (ushort)(EffectFlags & ~0x100);
        }

        public float PositionX
        {
            get => effect.F_12;
            set => SetValue(nameof(effect.F_12), effect.F_12, value, v => effect.F_12 = v, "BSA Effect Position X");
        }

        public float PositionY
        {
            get => effect.F_16;
            set => SetValue(nameof(effect.F_16), effect.F_16, value, v => effect.F_16 = v, "BSA Effect Position Y");
        }

        public float PositionZ
        {
            get => effect.F_20;
            set => SetValue(nameof(effect.F_20), effect.F_20, value, v => effect.F_20 = v, "BSA Effect Position Z");
        }

        public ushort AttachmentSelector
        {
            get => effect.I_06;
            set => SetValue(nameof(effect.I_06), effect.I_06, value, v => effect.I_06 = v, "BSA Effect Attachment Selector");
        }

        public ushort I_10
        {
            get => effect.I_10;
            set => SetValue(nameof(effect.I_10), effect.I_10, value, v => effect.I_10 = v, "BSA Effect I_10");
        }

        public BsaType6ViewModel(BSA_Type6 type) : base(type)
        {
            effect = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => EepkType);
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => EffectID);
            RaisePropertyChanged(() => Switch);
            RaisePropertyChanged(() => UseNestedEffectPosition);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => AttachmentSelector);
            RaisePropertyChanged(() => I_10);
        }
    }
}
