using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type18;

namespace XenoKit.ViewModel.BAC
{
    public class BACType18ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type18 bacType;

        public ushort PhysicsFunction
        {
            get
            {
                return (ushort)bacType.Function;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.Function), bacType, bacType.Function, (FunctionType)value, "PhysicsFunction"));
                bacType.Function = (FunctionType)value;
                RaisePropertyChanged(() => PhysicsFunction);
                bacType.RefreshType();
            }
        }
        public ushort EanID
        {
            get
            {
                return bacType.EanIndex;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.EanIndex), bacType, bacType.EanIndex, value, "PhysicsObjectControl EanID"));
                bacType.EanIndex = value;
                RaisePropertyChanged(() => EanID);
            }
        }
        public ushort PartMask
        {
            get
            {
                return bacType.PartMask;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.PartMask), bacType, bacType.PartMask, value, "PhysicsObjectControl PartMask"));
                bacType.PartMask = value;
                RaisePropertyChanged(() => PartMask);
            }
        }
        public ushort StartFrame
        {
            get
            {
                return bacType.StartFrame;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.StartFrame), bacType, bacType.StartFrame, value, "PhysicsObjectControl StartFrame"));
                bacType.StartFrame = value;
                RaisePropertyChanged(() => StartFrame);
            }
        }
        public float F_16
        {
            get
            {
                return bacType.F_16;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.F_16), bacType, bacType.F_16, value, "PhysicsObjectControl F_16"));
                bacType.F_16 = value;
                RaisePropertyChanged(() => F_16);
            }
        }
        public float F_20
        {
            get
            {
                return bacType.F_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.F_20), bacType, bacType.F_20, value, "PhysicsObjectControl F_20"));
                bacType.F_20 = value;
                RaisePropertyChanged(() => F_20);
            }
        }
        public float VectorScale
        {
            get
            {
                return bacType.VectorScale;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type18>(nameof(bacType.VectorScale), bacType, bacType.VectorScale, value, "PhysicsObjectControl VectorScale"));
                bacType.VectorScale = value;
                RaisePropertyChanged(() => VectorScale);
            }
        }



        public BACType18ViewModel(BAC_Type18 _bacType)
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
            RaisePropertyChanged(() => PhysicsFunction);
            RaisePropertyChanged(() => EanID);
            RaisePropertyChanged(() => PartMask);
            RaisePropertyChanged(() => StartFrame);
            RaisePropertyChanged(() => F_16);
            RaisePropertyChanged(() => F_20);
            RaisePropertyChanged(() => VectorScale);
            bacType.RefreshType();
        }

    }
}
