using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType7ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type7 sound;

        public AcbType AcbType
        {
            get => sound.AcbType;
            set
            {
                SetValue(nameof(sound.AcbType), sound.AcbType, value, v => sound.AcbType = v, "BSA Sound ACB Type");
            }
        }

        public ushort CueId
        {
            get => sound.CueId;
            set => SetValue(nameof(sound.CueId), sound.CueId, value, v => sound.CueId = v, "BSA Sound Cue ID");
        }

        private ushort SoundRoutingFlags
        {
            get => sound.I_02;
            set
            {
                SetValue(nameof(sound.I_02), sound.I_02, value, v => sound.I_02 = v, "BSA Sound Routing Flags");
                RaisePropertyChanged(() => AlternateSoundRoute);
                RaisePropertyChanged(() => CancelPendingSound);
            }
        }

        public bool AlternateSoundRoute
        {
            get => (sound.I_02 & 0x1000) != 0;
            set => SoundRoutingFlags = value ? (ushort)(sound.I_02 | 0x1000) : (ushort)(sound.I_02 & ~0x1000);
        }

        public bool CancelPendingSound
        {
            get => (sound.I_02 & 0x2000) != 0;
            set => SoundRoutingFlags = value ? (ushort)(sound.I_02 | 0x2000) : (ushort)(sound.I_02 & ~0x2000);
        }

        public ushort I_06
        {
            get => sound.I_06;
            set => SetValue(nameof(sound.I_06), sound.I_06, value, v => sound.I_06 = v, "BSA Sound I_06");
        }

        public BsaType7ViewModel(BSA_Type7 type) : base(type)
        {
            sound = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => AcbType);
            RaisePropertyChanged(() => CueId);
            RaisePropertyChanged(() => AlternateSoundRoute);
            RaisePropertyChanged(() => CancelPendingSound);
            RaisePropertyChanged(() => I_06);
        }
    }
}
