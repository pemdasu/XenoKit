using System.Windows;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType3ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type3 hitbox;

        public BsaShapeType ShapeType
        {
            get => hitbox.ShapeType;
            set
            {
                SetValue(nameof(hitbox.ShapeType), hitbox.ShapeType, value, v => hitbox.ShapeType = v, "BSA Hitbox Shape");
                RaiseShapeProperties();
            }
        }

        public string SizeLabel => hitbox.ShapeType == BsaShapeType.Box ? "Size X" : "Radius";
        public Visibility BoxVisibility => hitbox.ShapeType == BsaShapeType.Box ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CapsuleVisibility => hitbox.ShapeType == BsaShapeType.Capsule ? Visibility.Visible : Visibility.Collapsed;

        private ushort ShapeFlags
        {
            get => hitbox.ShapeFlags;
            set
            {
                SetValue(nameof(hitbox.ShapeFlags), hitbox.ShapeFlags, value, v => hitbox.ShapeFlags = v, "BSA Hitbox Shape Flags");
                RaisePropertyChanged(() => StretchToSpawnPoint);
                RaisePropertyChanged(() => HitAllOpponents);
                RaisePropertyChanged(() => UseWorldAxes);
            }
        }

        public bool StretchToSpawnPoint { get => (ShapeFlags & 0x1) != 0; set => SetShapeFlag(0x1, value); }
        public bool HitAllOpponents { get => (ShapeFlags & 0x2) != 0; set => SetShapeFlag(0x2, value); }
        public bool UseWorldAxes { get => (ShapeFlags & 0x4) != 0; set => SetShapeFlag(0x4, value); }

        public float PositionX { get => hitbox.F_08; set => SetValue(nameof(hitbox.F_08), hitbox.F_08, value, v => hitbox.F_08 = v, "BSA Hitbox Position X"); }
        public float PositionY { get => hitbox.F_12; set => SetValue(nameof(hitbox.F_12), hitbox.F_12, value, v => hitbox.F_12 = v, "BSA Hitbox Position Y"); }
        public float PositionZ { get => hitbox.F_16; set => SetValue(nameof(hitbox.F_16), hitbox.F_16, value, v => hitbox.F_16 = v, "BSA Hitbox Position Z"); }
        public float Size { get => hitbox.F_20; set => SetValue(nameof(hitbox.F_20), hitbox.F_20, value, v => hitbox.F_20 = v, "BSA Hitbox Size"); }

        public float BoxSizeY { get => hitbox.F_24; set => SetShapeValue(nameof(hitbox.F_24), hitbox.F_24, value, v => hitbox.F_24 = v, "BSA Hitbox Box Size Y"); }
        public float BoxSizeZ { get => hitbox.F_28; set => SetShapeValue(nameof(hitbox.F_28), hitbox.F_28, value, v => hitbox.F_28 = v, "BSA Hitbox Box Size Z"); }
        public float BoxRotation1 { get => hitbox.F_32; set => SetShapeValue(nameof(hitbox.F_32), hitbox.F_32, value, v => hitbox.F_32 = v, "BSA Hitbox Box Rotation 1"); }
        public float BoxRotation2 { get => hitbox.F_36; set => SetShapeValue(nameof(hitbox.F_36), hitbox.F_36, value, v => hitbox.F_36 = v, "BSA Hitbox Box Rotation 2"); }
        public float BoxRotation3 { get => hitbox.F_40; set => SetShapeValue(nameof(hitbox.F_40), hitbox.F_40, value, v => hitbox.F_40 = v, "BSA Hitbox Box Rotation 3"); }

        public float PointAX { get => hitbox.F_24; set => SetShapeValue(nameof(hitbox.F_24), hitbox.F_24, value, v => hitbox.F_24 = v, "BSA Hitbox Point A X"); }
        public float PointAY { get => hitbox.F_28; set => SetShapeValue(nameof(hitbox.F_28), hitbox.F_28, value, v => hitbox.F_28 = v, "BSA Hitbox Point A Y"); }
        public float PointAZ { get => hitbox.F_32; set => SetShapeValue(nameof(hitbox.F_32), hitbox.F_32, value, v => hitbox.F_32 = v, "BSA Hitbox Point A Z"); }
        public float PointBX { get => hitbox.F_36; set => SetShapeValue(nameof(hitbox.F_36), hitbox.F_36, value, v => hitbox.F_36 = v, "BSA Hitbox Point B X"); }
        public float PointBY { get => hitbox.F_40; set => SetShapeValue(nameof(hitbox.F_40), hitbox.F_40, value, v => hitbox.F_40 = v, "BSA Hitbox Point B Y"); }
        public float PointBZ { get => hitbox.F_44; set => SetShapeValue(nameof(hitbox.F_44), hitbox.F_44, value, v => hitbox.F_44 = v, "BSA Hitbox Point B Z"); }

        public byte HitLimit { get => hitbox.HitLimit; set => SetValue(nameof(hitbox.HitLimit), hitbox.HitLimit, value, v => hitbox.HitLimit = v, "BSA Hit Limit"); }
        public sbyte HitSlot { get => hitbox.HitSlot; set => SetValue(nameof(hitbox.HitSlot), hitbox.HitSlot, value, v => hitbox.HitSlot = v, "BSA Hit Counter Slot"); }
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
        public ushort HitFlags { get => hitbox.HitFlags; set => SetValue(nameof(hitbox.HitFlags), hitbox.HitFlags, value, v => hitbox.HitFlags = v, "BSA Hitbox I_06"); }

        public BsaType3ViewModel(BSA_Type3 type) : base(type)
        {
            hitbox = type;
        }

        private void SetShapeFlag(ushort mask, bool value)
        {
            ShapeFlags = value ? (ushort)(ShapeFlags | mask) : (ushort)(ShapeFlags & ~mask);
        }

        // Box size, box rotation and capsule points use the same model fields, so both sets update together.
        private void SetShapeValue(string modelProperty, float oldValue, float newValue, System.Action<float> assign, string undoName, [System.Runtime.CompilerServices.CallerMemberName] string viewModelProperty = null)
        {
            SetValue(modelProperty, oldValue, newValue, assign, undoName, viewModelProperty);
            RaiseShapeValues();
        }

        private void RaiseShapeProperties()
        {
            RaisePropertyChanged(() => ShapeType);
            RaisePropertyChanged(() => SizeLabel);
            RaisePropertyChanged(() => BoxVisibility);
            RaisePropertyChanged(() => CapsuleVisibility);
        }

        private void RaiseShapeValues()
        {
            RaisePropertyChanged(() => BoxSizeY);
            RaisePropertyChanged(() => BoxSizeZ);
            RaisePropertyChanged(() => BoxRotation1);
            RaisePropertyChanged(() => BoxRotation2);
            RaisePropertyChanged(() => BoxRotation3);
            RaisePropertyChanged(() => PointAX);
            RaisePropertyChanged(() => PointAY);
            RaisePropertyChanged(() => PointAZ);
            RaisePropertyChanged(() => PointBX);
            RaisePropertyChanged(() => PointBY);
            RaisePropertyChanged(() => PointBZ);
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaiseShapeProperties();
            RaiseShapeValues();
            RaisePropertyChanged(() => StretchToSpawnPoint);
            RaisePropertyChanged(() => HitAllOpponents);
            RaisePropertyChanged(() => UseWorldAxes);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => Size);
            RaisePropertyChanged(() => HitLimit);
            RaisePropertyChanged(() => HitSlot);
            RaisePropertyChanged(() => HitboxPriority);
            RaisePropertyChanged(() => TargetLinkedActorOnly);
            RaisePropertyChanged(() => FirstHit);
            RaisePropertyChanged(() => MultipleHits);
            RaisePropertyChanged(() => LastHit);
            RaisePropertyChanged(() => I_02);
            RaisePropertyChanged(() => HitFlags);
        }
    }
}
