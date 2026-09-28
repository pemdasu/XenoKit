using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType1ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type1 movement;

        public int Operation
        {
            get => movement.I_00 & 0xFF;
            set
            {
                int newValue = (movement.I_00 & ~0xFF) | (value & 0xFF);
                SetValue(nameof(movement.I_00), movement.I_00, newValue, v => movement.I_00 = v, "BSA Movement Operation");
                UpdateProperties();
            }
        }

        public string OperationDisplayName => Xv2CoreLib.ValuesDictionary.BSA.MovementOperation.TryGetValue(Operation, out string name)
            ? name : $"Unknown ({Operation})";

        public bool SetPrimaryMovementState
        {
            get => (movement.I_00 & 0x00020000) != 0;
            set => SetMotionFlag(0x00020000, value);
        }

        public bool ObjectRelativeDirection
        {
            get => (movement.I_00 & 0x00200000) != 0;
            set => SetMotionFlag(0x00200000, value);
        }

        public bool UseRecordMovementVector { get => (movement.I_00 & 0x00010000) != 0; set => SetMotionFlag(0x00010000, value); }
        public bool ClearPrimaryMovementState { get => (movement.I_00 & 0x00040000) != 0; set => SetMotionFlag(0x00040000, value); }
        public bool SetScalarMovementState { get => (movement.I_00 & 0x00080000) != 0; set => SetMotionFlag(0x00080000, value); }
        public bool ClearScalarMovementState { get => (movement.I_00 & 0x00100000) != 0; set => SetMotionFlag(0x00100000, value); }
        public bool InterpolateLinkedDistance { get => (movement.I_00 & 0x00400000) != 0; set => SetMotionFlag(0x00400000, value); }
        public bool AlternateSpreadPath { get => (movement.I_00 & 0x00800000) != 0; set => SetMotionFlag(0x00800000, value); }
        public bool SetSecondaryMovementState { get => (movement.I_00 & 0x01000000) != 0; set => SetMotionFlag(0x01000000, value); }
        public bool ClearSecondaryMovementState { get => (movement.I_00 & 0x02000000) != 0; set => SetMotionFlag(0x02000000, value); }
        public bool UseContextBasis { get => (movement.I_00 & 0x04000000) != 0; set => SetMotionFlag(0x04000000, value); }
        public bool RefreshLinkedObject { get => (movement.I_00 & 0x08000000) != 0; set => SetMotionFlag(0x08000000, value); }
        public bool SetAlternateMovementState { get => (movement.I_00 & 0x10000000) != 0; set => SetMotionFlag(0x10000000, value); }
        public bool UnknownMotionFlag2000 { get => (movement.I_00 & 0x20000000) != 0; set => SetMotionFlag(0x20000000, value); }
        public bool UnknownMotionFlag4000 { get => (movement.I_00 & 0x40000000) != 0; set => SetMotionFlag(0x40000000, value); }
        public bool RandomizeSpreadSign { get => (movement.I_00 & unchecked((int)0x80000000)) != 0; set => SetMotionFlag(unchecked((int)0x80000000), value); }

        public float Speed { get => movement.F_04; set => SetValue(nameof(movement.F_04), movement.F_04, value, v => movement.F_04 = v, "BSA Speed"); }
        public float DirectionX { get => movement.F_08; set => SetValue(nameof(movement.F_08), movement.F_08, value, v => movement.F_08 = v, "BSA Direction X"); }
        public float DirectionY { get => movement.F_12; set => SetValue(nameof(movement.F_12), movement.F_12, value, v => movement.F_12 = v, "BSA Direction Y"); }
        public float DirectionZ { get => movement.F_16; set => SetValue(nameof(movement.F_16), movement.F_16, value, v => movement.F_16 = v, "BSA Direction Z"); }
        public float HomingWeightX { get => movement.F_20; set => SetValue(nameof(movement.F_20), movement.F_20, value, v => movement.F_20 = v, "BSA Homing Weight X"); }
        public float HomingWeightY { get => movement.F_24; set => SetValue(nameof(movement.F_24), movement.F_24, value, v => movement.F_24 = v, "BSA Homing Weight Y"); }
        public float HomingWeightZ { get => movement.F_28; set => SetValue(nameof(movement.F_28), movement.F_28, value, v => movement.F_28 = v, "BSA Homing Weight Z"); }
        public float MaxSpreadDistance { get => movement.F_32; set => SetValue(nameof(movement.F_32), movement.F_32, value, v => movement.F_32 = v, "BSA Max Spread Distance"); }
        public float MinSpreadDistance { get => movement.F_36; set => SetValue(nameof(movement.F_36), movement.F_36, value, v => movement.F_36 = v, "BSA Min Spread Distance"); }
        public float MaxSpreadAngle { get => movement.F_40; set => SetValue(nameof(movement.F_40), movement.F_40, value, v => movement.F_40 = v, "BSA Max Spread Angle"); }
        public float MinSpreadAngle { get => movement.F_44; set => SetValue(nameof(movement.F_44), movement.F_44, value, v => movement.F_44 = v, "BSA Min Spread Angle"); }

        public BsaType1ViewModel(BSA_Type1 type) : base(type)
        {
            movement = type;
        }

        private void SetMotionFlag(int mask, bool value)
        {
            int newValue = value ? movement.I_00 | mask : movement.I_00 & ~mask;
            SetValue(nameof(movement.I_00), movement.I_00, newValue, v => movement.I_00 = v, "BSA Motion Flags");
            UpdateProperties();
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => Operation);
            RaisePropertyChanged(() => OperationDisplayName);
            RaisePropertyChanged(() => SetPrimaryMovementState);
            RaisePropertyChanged(() => ObjectRelativeDirection);
            RaisePropertyChanged(() => UseRecordMovementVector);
            RaisePropertyChanged(() => ClearPrimaryMovementState);
            RaisePropertyChanged(() => SetScalarMovementState);
            RaisePropertyChanged(() => ClearScalarMovementState);
            RaisePropertyChanged(() => InterpolateLinkedDistance);
            RaisePropertyChanged(() => AlternateSpreadPath);
            RaisePropertyChanged(() => SetSecondaryMovementState);
            RaisePropertyChanged(() => ClearSecondaryMovementState);
            RaisePropertyChanged(() => UseContextBasis);
            RaisePropertyChanged(() => RefreshLinkedObject);
            RaisePropertyChanged(() => SetAlternateMovementState);
            RaisePropertyChanged(() => UnknownMotionFlag2000);
            RaisePropertyChanged(() => UnknownMotionFlag4000);
            RaisePropertyChanged(() => RandomizeSpreadSign);
            RaisePropertyChanged(() => Speed);
            RaisePropertyChanged(() => DirectionX);
            RaisePropertyChanged(() => DirectionY);
            RaisePropertyChanged(() => DirectionZ);
            RaisePropertyChanged(() => HomingWeightX);
            RaisePropertyChanged(() => HomingWeightY);
            RaisePropertyChanged(() => HomingWeightZ);
            RaisePropertyChanged(() => MaxSpreadDistance);
            RaisePropertyChanged(() => MinSpreadDistance);
            RaisePropertyChanged(() => MaxSpreadAngle);
            RaisePropertyChanged(() => MinSpreadAngle);
        }
    }
}
