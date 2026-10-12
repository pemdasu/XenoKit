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
            get => passEntry.Condition;
            set => SetValue(nameof(passEntry.Condition), passEntry.Condition, value, v => passEntry.Condition = v, "BSA Entry Passing Condition");
        }

        private ushort ConditionFlags
        {
            get => passEntry.ConditionFlags;
            set
            {
                SetValue(nameof(passEntry.ConditionFlags), passEntry.ConditionFlags, value, v => passEntry.ConditionFlags = v, "BSA Entry Passing Flags");
                RaisePropertyChanged(() => SkipEntryCall);
                RaisePropertyChanged(() => SkipTimingUpdate);
                RaisePropertyChanged(() => CompareSignalValue);
                RaisePropertyChanged(() => ReceivePrioritySignal);
            }
        }

        public bool SkipEntryCall { get => (ConditionFlags & 0x1) != 0; set => SetConditionFlag(0x1, value); }
        public bool SkipTimingUpdate { get => (ConditionFlags & 0x2) != 0; set => SetConditionFlag(0x2, value); }
        public bool CompareSignalValue { get => (ConditionFlags & 0x8) != 0; set => SetConditionFlag(0x8, value); }
        public bool ReceivePrioritySignal { get => (ConditionFlags & 0x10) != 0; set => SetConditionFlag(0x10, value); }

        public float ConditionValue
        {
            get => passEntry.ConditionValue;
            set => SetValue(nameof(passEntry.ConditionValue), passEntry.ConditionValue, value, v => passEntry.ConditionValue = v, "BSA Pass Condition Value");
        }

        public short JumpFrame
        {
            get => passEntry.JumpFrame;
            set => SetValue(nameof(passEntry.JumpFrame), passEntry.JumpFrame, value, v => passEntry.JumpFrame = v, "BSA Pass Jump Frame");
        }

        public float SignalPriority
        {
            get => passEntry.SignalPriority;
            set => SetValue(nameof(passEntry.SignalPriority), passEntry.SignalPriority, value, v => passEntry.SignalPriority = v, "BSA Pass Signal Priority");
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
            RaisePropertyChanged(() => ReceivePrioritySignal);
            RaisePropertyChanged(() => ConditionValue);
            RaisePropertyChanged(() => JumpFrame);
            RaisePropertyChanged(() => SignalPriority);
        }
    }
}
