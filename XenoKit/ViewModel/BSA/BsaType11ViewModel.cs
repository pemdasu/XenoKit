using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType11ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type11 type;

        public ushort SkillID { get => type.SkillID; set => SetValue(nameof(type.SkillID), type.SkillID, value, v => type.SkillID = v, "BSA Effect Property Skill ID"); }
        public ushort SkillType { get => type.SkillType; set => SetValue(nameof(type.SkillType), type.SkillType, value, v => type.SkillType = v, "BSA Effect Property Skill Type"); }
        public ushort EffectID { get => type.EffectID; set => SetValue(nameof(type.EffectID), type.EffectID, value, v => type.EffectID = v, "BSA Effect Property Effect ID"); }
        public ushort FunctionDuration { get => type.FunctionDuration; set => SetValue(nameof(type.FunctionDuration), type.FunctionDuration, value, v => type.FunctionDuration = v, "BSA Effect Property Duration"); }

        private ushort Function
        {
            get => type.Function;
            set
            {
                SetValue(nameof(type.Function), type.Function, value, v => type.Function = v, "BSA Effect Property Flags");
                RaisePropertyChanged(() => ApplyProperties);
                RaisePropertyChanged(() => Property1);
                RaisePropertyChanged(() => Property2);
                RaisePropertyChanged(() => Property3);
            }
        }

        public bool ApplyProperties { get => (Function & 0x1) != 0; set => SetFlag(0x1, value); }
        public bool Property1 { get => (Function & 0x4) != 0; set => SetFlag(0x4, value); }
        public bool Property2 { get => (Function & 0x8) != 0; set => SetFlag(0x8, value); }
        public bool Property3 { get => (Function & 0x10) != 0; set => SetFlag(0x10, value); }

        public ushort I_10 { get => type.I_10; set => SetValue(nameof(type.I_10), type.I_10, value, v => type.I_10 = v, "BSA Effect Property I_10"); }
        public ushort I_12 { get => type.I_12; set => SetValue(nameof(type.I_12), type.I_12, value, v => type.I_12 = v, "BSA Effect Property I_12"); }
        public ushort I_14 { get => type.I_14; set => SetValue(nameof(type.I_14), type.I_14, value, v => type.I_14 = v, "BSA Effect Property I_14"); }

        public BsaType11ViewModel(BSA_Type11 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => SkillType);
            RaisePropertyChanged(() => EffectID);
            RaisePropertyChanged(() => FunctionDuration);
            RaisePropertyChanged(() => ApplyProperties);
            RaisePropertyChanged(() => Property1);
            RaisePropertyChanged(() => Property2);
            RaisePropertyChanged(() => Property3);
            RaisePropertyChanged(() => I_10);
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => I_14);
        }

        private void SetFlag(ushort flag, bool value)
        {
            Function = value ? (ushort)(Function | flag) : (ushort)(Function & ~flag);
        }
    }
}
