using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType0ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type0 passEntry;

        public ushort BsaEntryId
        {
            get => passEntry.BSA_EntryID;
            set => SetValue(nameof(passEntry.BSA_EntryID), passEntry.BSA_EntryID, value, v => passEntry.BSA_EntryID = v, "BSA Pass Entry");
        }

        public short Condition
        {
            get => passEntry.I_00;
            set => SetValue(nameof(passEntry.I_00), passEntry.I_00, value, v => passEntry.I_00 = v, "BSA Entry Passing Condition");
        }

        private ushort ConditionFlags
        {
            get => passEntry.I_02;
            set
            {
                SetValue(nameof(passEntry.I_02), passEntry.I_02, value, v => passEntry.I_02 = v, "BSA Entry Passing Flags");
                RaisePropertyChanged(() => SkipEntryCall);
                RaisePropertyChanged(() => SkipTimingUpdate);
                RaisePropertyChanged(() => CompareSignalValue);
                RaisePropertyChanged(() => ReceiveBacCondition);
            }
        }

        public bool SkipEntryCall { get => (ConditionFlags & 0x1) != 0; set => SetConditionFlag(0x1, value); }
        public bool SkipTimingUpdate { get => (ConditionFlags & 0x2) != 0; set => SetConditionFlag(0x2, value); }
        public bool CompareSignalValue { get => (ConditionFlags & 0x8) != 0; set => SetConditionFlag(0x8, value); }
        public bool ReceiveBacCondition { get => (ConditionFlags & 0x10) != 0; set => SetConditionFlag(0x10, value); }

        public float BacCondition
        {
            get => passEntry.F_08;
            set => SetValue(nameof(passEntry.F_08), passEntry.F_08, value, v => passEntry.F_08 = v, "BSA Pass BAC Condition");
        }

        public short I_06
        {
            get => passEntry.I_06;
            set => SetValue(nameof(passEntry.I_06), passEntry.I_06, value, v => passEntry.I_06 = v, "BSA Pass I_06");
        }

        public float F_12
        {
            get => passEntry.F_12;
            set => SetValue(nameof(passEntry.F_12), passEntry.F_12, value, v => passEntry.F_12 = v, "BSA Pass F_12");
        }

        public BsaType0ViewModel(BSA_Type0 type) : base(type)
        {
            passEntry = type;
        }

        private void SetConditionFlag(ushort mask, bool value)
        {
            ConditionFlags = value ? (ushort)(ConditionFlags | mask) : (ushort)(ConditionFlags & ~mask);
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => BsaEntryId);
            RaisePropertyChanged(() => Condition);
            RaisePropertyChanged(() => SkipEntryCall);
            RaisePropertyChanged(() => SkipTimingUpdate);
            RaisePropertyChanged(() => CompareSignalValue);
            RaisePropertyChanged(() => ReceiveBacCondition);
            RaisePropertyChanged(() => BacCondition);
            RaisePropertyChanged(() => I_06);
            RaisePropertyChanged(() => F_12);
        }
    }
}
