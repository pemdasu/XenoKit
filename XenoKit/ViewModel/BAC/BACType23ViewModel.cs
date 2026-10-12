using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using XenoKit.Helper.Find;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type23;

namespace XenoKit.ViewModel.BAC
{
    public class BACType23ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type23 bacType;

        public string DrawTypeLabel => Enum.IsDefined(typeof(DrawTypeEnum), DrawType) ? DrawType.ToString() : $"Unknown ({(byte)DrawType})";

        public byte HorizontalLineSize
        {
            get
            {
                return bacType.HorizontalLineSize;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.HorizontalLineSize), bacType, bacType.HorizontalLineSize, value, "Horizontal Line Size"));
                bacType.HorizontalLineSize = value;
                RaisePropertyChanged(() => HorizontalLineSize);
            }
        }
        public byte VerticalLineSize
        {
            get
            {
                return bacType.VerticalLineSize;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.VerticalLineSize), bacType, bacType.VerticalLineSize, value, "Vertical Line Size"));
                bacType.VerticalLineSize = value;
                RaisePropertyChanged(() => VerticalLineSize);
            }
        }
        public byte HorizontalLineSpacing
        {
            get
            {
                return bacType.HorizontalLineSpacing;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.HorizontalLineSpacing), bacType, bacType.HorizontalLineSpacing, value, "Horizontal Line Spacing"));
                bacType.HorizontalLineSpacing = value;
                RaisePropertyChanged(() => HorizontalLineSpacing);
            }
        }
        public byte VerticalLineSpacing
        {
            get
            {
                return bacType.VerticalLineSpacing;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.VerticalLineSpacing), bacType, bacType.VerticalLineSpacing, value, "Vertical Line Spacing"));
                bacType.VerticalLineSpacing = value;
                RaisePropertyChanged(() => VerticalLineSpacing);
            }
        }
        public byte HorizontalPatternShift
        {
            get => bacType.HorizontalPatternShift;
            set
            {
                if (bacType.HorizontalPatternShift == value) return;
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.HorizontalPatternShift), bacType, bacType.HorizontalPatternShift, value, "Horizontal Pattern Shift"));
                bacType.HorizontalPatternShift = value;
                RaisePropertyChanged(() => HorizontalPatternShift);
            }
        }
        public float StripeBrightness
        {
            get => bacType.StripeBrightness;
            set
            {
                if (bacType.StripeBrightness == value) return;
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.StripeBrightness), bacType, bacType.StripeBrightness, value, "Stripe Brightness"));
                bacType.StripeBrightness = value;
                RaisePropertyChanged(() => StripeBrightness);
            }
        }
        public byte VerticalPatternShift
        {
            get
            {
                return bacType.VerticalPatternShift;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.VerticalPatternShift), bacType, bacType.VerticalPatternShift, value, "Vertical Pattern Shift"));
                bacType.VerticalPatternShift = value;
                RaisePropertyChanged(() => VerticalPatternShift);
            }
        }
        public byte StripeHeight
        {
            get
            {
                return bacType.StripeHeight;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.StripeHeight), bacType, bacType.StripeHeight, value, "Stripe Height"));
                bacType.StripeHeight = value;
                RaisePropertyChanged(() => StripeHeight);
            }
        }

        public Color Tint
        {
            get
            {
                return Color.FromArgb((byte)(bacType.Tint_A * 255f), (byte)(bacType.Tint_R * 255f), (byte)(bacType.Tint_G * 255f), (byte)(bacType.Tint_B * 255f));
            }
            set
            {
                float r = value.R / 255f;
                float g = value.G / 255f;
                float b = value.B / 255f;
                float a = value.A / 255f;

                if (r != bacType.Tint_R || g != bacType.Tint_G || b != bacType.Tint_B || a != bacType.Tint_A)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    undos.Add(new UndoableProperty<BAC_Type23>(nameof(bacType.Tint_R), bacType, bacType.Tint_R, r));
                    undos.Add(new UndoableProperty<BAC_Type23>(nameof(bacType.Tint_G), bacType, bacType.Tint_G, g));
                    undos.Add(new UndoableProperty<BAC_Type23>(nameof(bacType.Tint_B), bacType, bacType.Tint_B, b));
                    undos.Add(new UndoableProperty<BAC_Type23>(nameof(bacType.Tint_A), bacType, bacType.Tint_A, a));

                    bacType.Tint_R = r;
                    bacType.Tint_G = g;
                    bacType.Tint_B = b;
                    bacType.Tint_A = a;

                    UndoManager.Instance.AddCompositeUndo(undos, "Transparency Tint Color");

                    RaisePropertyChanged(() => Tint);
                }
            }
        }

        //Unknown floats
        public float Desaturation
        {
            get
            {
                return bacType.Desaturation;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.Desaturation), bacType, bacType.Desaturation, value, "Transparency Desaturation"));
                bacType.Desaturation = value;
                RaisePropertyChanged(() => Desaturation);
            }
        }
        public float RimTintStrength
        {
            get
            {
                return bacType.RimTintStrength;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.RimTintStrength), bacType, bacType.RimTintStrength, value, "Rim Tint Strength"));
                bacType.RimTintStrength = value;
                RaisePropertyChanged(() => RimTintStrength);
            }
        }
        public float RimTintPower
        {
            get
            {
                return bacType.RimTintPower;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.RimTintPower), bacType, bacType.RimTintPower, value, "Rim Tint Power"));
                bacType.RimTintPower = value;
                RaisePropertyChanged(() => RimTintPower);
            }
        }
        public float F_48
        {
            get
            {
                return bacType.F_48;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.F_48), bacType, bacType.F_48, value, "Transparency F_48"));
                bacType.F_48 = value;
                RaisePropertyChanged(() => F_48);
            }
        }

        //Shader Path Options
        public DrawTypeEnum DrawType
        {
            get
            {
                return bacType.DrawType;
            }
            set
            {
                if (bacType.DrawType == value) return;
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type23>(nameof(bacType.DrawType), bacType, bacType.DrawType, value, "Draw Type"));
                bacType.DrawType = value;
                RaisePropertyChanged(() => DrawType);
                RaisePropertyChanged(() => DrawTypeLabel);
            }
        }

        public BACType23ViewModel(BAC_Type23 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            bacType.PropertyChanged -= BacType_PropertyChanged;
        }

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        private void BacType_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => HorizontalLineSize);
            RaisePropertyChanged(() => VerticalLineSize);
            RaisePropertyChanged(() => HorizontalLineSpacing);
            RaisePropertyChanged(() => VerticalLineSpacing);
            RaisePropertyChanged(() => HorizontalPatternShift);
            RaisePropertyChanged(() => StripeBrightness);
            RaisePropertyChanged(() => VerticalPatternShift);
            RaisePropertyChanged(() => StripeHeight);
            RaisePropertyChanged(() => Desaturation);
            RaisePropertyChanged(() => RimTintStrength);
            RaisePropertyChanged(() => RimTintPower);
            RaisePropertyChanged(() => F_48);
            RaisePropertyChanged(() => Tint);
            RaisePropertyChanged(() => DrawType);
            RaisePropertyChanged(() => DrawTypeLabel);
        }
    }
}
