using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType9ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type9 type;

        public float HealthValue
        {
            get => type.HealthValue;
            set => SetValue(nameof(type.HealthValue), type.HealthValue, value, v => type.HealthValue = v, "BSA Projectile Health Value");
        }

        private uint Flags
        {
            get => type.Flags;
            set
            {
                SetValue(nameof(type.Flags), type.Flags, value, v => type.Flags = v, "BSA Projectile Health Flags");
                RaisePropertyChanged(() => AddToValue);
                RaisePropertyChanged(() => UseMaximumHealth);
                RaisePropertyChanged(() => UsePercentage);
            }
        }

        public bool AddToValue
        {
            get => (Flags & 0x1) != 0;
            set => SetFlag(0x1, value);
        }

        public bool UseMaximumHealth
        {
            get => (Flags & 0x2) != 0;
            set => SetFlag(0x2, value);
        }

        public bool UsePercentage
        {
            get => (Flags & 0x4) != 0;
            set => SetFlag(0x4, value);
        }

        public BsaType9ViewModel(BSA_Type9 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => HealthValue);
            RaisePropertyChanged(() => AddToValue);
            RaisePropertyChanged(() => UseMaximumHealth);
            RaisePropertyChanged(() => UsePercentage);
        }

        private void SetFlag(uint flag, bool value)
        {
            Flags = value ? Flags | flag : Flags & ~flag;
        }
    }
}
