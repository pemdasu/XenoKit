using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType10ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type10 type;

        public short SkillID
        {
            get => type.SkillID;
            set => SetValue(nameof(type.SkillID), type.SkillID, value, v => type.SkillID = v, "BSA Skill ID");
        }

        public short SkillType
        {
            get => type.SkillType;
            set => SetValue(nameof(type.SkillType), type.SkillType, value, v => type.SkillType = v, "BSA Skill Type");
        }

        public short UpgradeLevelDelta
        {
            get => type.UpgradeLevelDelta;
            set => SetValue(nameof(type.UpgradeLevelDelta), type.UpgradeLevelDelta, value, v => type.UpgradeLevelDelta = v, "BSA Upgrade Level Delta");
        }

        public byte UpgradeOperation
        {
            get => type.UpgradeOperation;
            set => SetValue(nameof(type.UpgradeOperation), type.UpgradeOperation, value, v => type.UpgradeOperation = v, "BSA Upgrade Operation");
        }

        public BsaType10ViewModel(BSA_Type10 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => SkillType);
            RaisePropertyChanged(() => UpgradeLevelDelta);
            RaisePropertyChanged(() => UpgradeOperation);
        }
    }
}
