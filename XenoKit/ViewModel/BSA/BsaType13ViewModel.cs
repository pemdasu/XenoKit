using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType13ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type13 type;

        public ProjectileProtectionOperation ProtectionState
        {
            get => type.I_00;
            set => SetValue(nameof(type.I_00), type.I_00, value, v => type.I_00 = v, "BSA Projectile Protection State");
        }

        public float MaxHitboxPriority
        {
            get => type.F_04;
            set => SetValue(nameof(type.F_04), type.F_04, value, v => type.F_04 = v, "BSA Projectile Protection Max Hitbox Priority");
        }

        public bool ProtectSelectors0To3
        {
            get => type.F_08 != 0f;
            set => SetValue(nameof(type.F_08), type.F_08, value ? 1f : 0f, v => type.F_08 = v, "BSA Projectile Protection Selectors 0-3");
        }

        public float AdditionalSelectorCoverage
        {
            get => type.I_12;
            set => SetValue(nameof(type.I_12), type.I_12, value, v => type.I_12 = v, "BSA Projectile Protection Additional Selectors");
        }

        public float EntryPassingSignalValue
        {
            get => type.F_16;
            set => SetValue(nameof(type.F_16), type.F_16, value, v => type.F_16 = v, "BSA Projectile Protection Entry Passing Signal");
        }

        public bool MarkProtectedHit
        {
            get => type.I_20 != 0f;
            set => SetValue(nameof(type.I_20), type.I_20, value ? 1f : 0f, v => type.I_20 = v, "BSA Projectile Protection Mark Protected Hit");
        }

        public BsaType13ViewModel(BSA_Type13 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => ProtectionState);
            RaisePropertyChanged(() => MaxHitboxPriority);
            RaisePropertyChanged(() => ProtectSelectors0To3);
            RaisePropertyChanged(() => AdditionalSelectorCoverage);
            RaisePropertyChanged(() => EntryPassingSignalValue);
            RaisePropertyChanged(() => MarkProtectedHit);
        }
    }
}
