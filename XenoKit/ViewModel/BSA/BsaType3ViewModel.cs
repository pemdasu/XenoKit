using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType3ViewModel : BsaTypeBaseViewModel
    {
        private const ushort BoundsTypeMask = 0x000F;
        private readonly BSA_Type3 hitbox;

        // There is no ValuesDictionary entry for this enum, so the view binds the enum values directly.
        public Array BoundingBoxTypes => Enum.GetValues(typeof(BAC_Type1.BoundingBoxTypeEnum));

        public BAC_Type1.BoundingBoxTypeEnum BoundingBoxType
        {
            get => (BAC_Type1.BoundingBoxTypeEnum)(hitbox.I_00 & BoundsTypeMask);
            set
            {
                ushort newFlags = (ushort)((hitbox.I_00 & ~BoundsTypeMask) | ((ushort)value & BoundsTypeMask));
                SetValue(nameof(hitbox.I_00), hitbox.I_00, newFlags, v => hitbox.I_00 = v, "BSA Hitbox Bounds Type");
                RaiseBoundsProperties();
            }
        }

        public bool BoundsEnabled => BoundingBoxType != BAC_Type1.BoundingBoxTypeEnum.Uniform;

        public Switch UseDistanceRelativeBounds
        {
            get => (hitbox.I_04 & 1) == 0 ? Switch.Off : Switch.On;
            set
            {
                ushort newValue = (ushort)((hitbox.I_04 & ~1) | (value == Switch.On ? 1 : 0));
                SetValue(nameof(hitbox.I_04), hitbox.I_04, newValue, v => hitbox.I_04 = v, "BSA Distance Relative Bounds");
            }
        }

        public float PositionX { get => hitbox.F_08; set => SetValue(nameof(hitbox.F_08), hitbox.F_08, value, v => hitbox.F_08 = v, "BSA Hitbox Position X"); }
        public float PositionY { get => hitbox.F_12; set => SetValue(nameof(hitbox.F_12), hitbox.F_12, value, v => hitbox.F_12 = v, "BSA Hitbox Position Y"); }
        public float PositionZ { get => hitbox.F_16; set => SetValue(nameof(hitbox.F_16), hitbox.F_16, value, v => hitbox.F_16 = v, "BSA Hitbox Position Z"); }
        public float Size { get => hitbox.F_20; set => SetValue(nameof(hitbox.F_20), hitbox.F_20, value, v => hitbox.F_20 = v, "BSA Hitbox Scale"); }
        public float MaxX { get => hitbox.F_24; set => SetValue(nameof(hitbox.F_24), hitbox.F_24, value, v => hitbox.F_24 = v, "BSA Hitbox Max X"); }
        public float MaxY { get => hitbox.F_28; set => SetValue(nameof(hitbox.F_28), hitbox.F_28, value, v => hitbox.F_28 = v, "BSA Hitbox Max Y"); }
        public float MaxZ { get => hitbox.F_32; set => SetValue(nameof(hitbox.F_32), hitbox.F_32, value, v => hitbox.F_32 = v, "BSA Hitbox Max Z"); }
        public float MinX { get => hitbox.F_36; set => SetValue(nameof(hitbox.F_36), hitbox.F_36, value, v => hitbox.F_36 = v, "BSA Hitbox Min X"); }
        public float MinY { get => hitbox.F_40; set => SetValue(nameof(hitbox.F_40), hitbox.F_40, value, v => hitbox.F_40 = v, "BSA Hitbox Min Y"); }
        public float MinZ { get => hitbox.F_44; set => SetValue(nameof(hitbox.F_44), hitbox.F_44, value, v => hitbox.F_44 = v, "BSA Hitbox Min Z"); }

        public ushort HitCountControl { get => hitbox.I_48; set => SetValue(nameof(hitbox.I_48), hitbox.I_48, value, v => hitbox.I_48 = v, "BSA Hit Count Control"); }
        public ushort HitboxPriority { get => hitbox.I_50; set => SetValue(nameof(hitbox.I_50), hitbox.I_50, value, v => hitbox.I_50 = v, "BSA Hitbox Priority"); }
        public bool TargetLinkedActorOnly
        {
            get => (hitbox.I_52 & 1) != 0;
            set
            {
                ushort newValue = (ushort)((hitbox.I_52 & ~1) | (value ? 1 : 0));
                SetValue(nameof(hitbox.I_52), hitbox.I_52, newValue, v => hitbox.I_52 = v, "BSA Target Linked Actor Only");
            }
        }

        public ushort FirstHit { get => hitbox.FirstHit; set => SetValue(nameof(hitbox.FirstHit), hitbox.FirstHit, value, v => hitbox.FirstHit = v, "BSA Hitbox BDM First Hit ID"); }
        public ushort MultipleHits { get => hitbox.MultipleHits; set => SetValue(nameof(hitbox.MultipleHits), hitbox.MultipleHits, value, v => hitbox.MultipleHits = v, "BSA Hitbox BDM Multiple Hits ID"); }
        public ushort LastHit { get => hitbox.LastHit; set => SetValue(nameof(hitbox.LastHit), hitbox.LastHit, value, v => hitbox.LastHit = v, "BSA Hitbox BDM Last Hit ID"); }

        public ushort I_02 { get => hitbox.I_02; set => SetValue(nameof(hitbox.I_02), hitbox.I_02, value, v => hitbox.I_02 = v, "BSA Hitbox I_02"); }
        public byte I_06_a { get => hitbox.I_06_a; set => SetValue(nameof(hitbox.I_06_a), hitbox.I_06_a, value, v => hitbox.I_06_a = v, "BSA Hitbox I_06_a"); }
        public byte I_06_b { get => hitbox.I_06_b; set => SetValue(nameof(hitbox.I_06_b), hitbox.I_06_b, value, v => hitbox.I_06_b = v, "BSA Hitbox I_06_b"); }
        public byte I_06_c { get => hitbox.I_06_c; set => SetValue(nameof(hitbox.I_06_c), hitbox.I_06_c, value, v => hitbox.I_06_c = v, "BSA Hitbox I_06_c"); }
        public byte I_06_d { get => hitbox.I_06_d; set => SetValue(nameof(hitbox.I_06_d), hitbox.I_06_d, value, v => hitbox.I_06_d = v, "BSA Hitbox I_06_d"); }

        public BsaType3ViewModel(BSA_Type3 type) : base(type)
        {
            hitbox = type;
        }

        private void RaiseBoundsProperties()
        {
            RaisePropertyChanged(() => BoundingBoxType);
            RaisePropertyChanged(() => BoundsEnabled);
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaiseBoundsProperties();
            RaisePropertyChanged(() => UseDistanceRelativeBounds);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => Size);
            RaisePropertyChanged(() => MaxX);
            RaisePropertyChanged(() => MaxY);
            RaisePropertyChanged(() => MaxZ);
            RaisePropertyChanged(() => MinX);
            RaisePropertyChanged(() => MinY);
            RaisePropertyChanged(() => MinZ);
            RaisePropertyChanged(() => HitCountControl);
            RaisePropertyChanged(() => HitboxPriority);
            RaisePropertyChanged(() => TargetLinkedActorOnly);
            RaisePropertyChanged(() => FirstHit);
            RaisePropertyChanged(() => MultipleHits);
            RaisePropertyChanged(() => LastHit);
            RaisePropertyChanged(() => I_02);
            RaisePropertyChanged(() => I_06_a);
            RaisePropertyChanged(() => I_06_b);
            RaisePropertyChanged(() => I_06_c);
            RaisePropertyChanged(() => I_06_d);
        }
    }
}
