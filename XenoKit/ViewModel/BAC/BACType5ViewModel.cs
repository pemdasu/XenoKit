using GalaSoft.MvvmLight;
using System;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type5;

namespace XenoKit.ViewModel.BAC
{
    public class BACType5ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type5 bacType;

        public float Tracking
        {
            get
            {
                return bacType.Tracking;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.Tracking), bacType, bacType.Tracking, value, "Tracking"));
                bacType.Tracking = value;
                RaisePropertyChanged(() => Tracking);
            }
        }

        //TrackingFlags
        public bool TrackingFlag_AimAtLockedTarget
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.AimAtLockedTarget);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.AimAtLockedTarget, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_AimAtLockedTarget);
                }
            }
        }
        public bool TrackingFlag_NoYaw
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.NoYaw);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.NoYaw, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_NoYaw);
                }
            }
        }
        public bool TrackingFlag_Unk10
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.Unk10);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.Unk10, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_Unk10);
                }
            }
        }
        public bool TrackingFlag_TrackForwardAndBackward
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.TrackForwardAndBackwards);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.TrackForwardAndBackwards, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_TrackForwardAndBackward);
                }
            }
        }
        public bool TrackingFlag_Unk12
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.Unk12);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.Unk12, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_Unk12);
                }
            }
        }
        public bool TrackingFlag_ReverseYaw
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.ReverseYaw);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.ReverseYaw, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_ReverseYaw);
                }
            }
        }
        public bool TrackingFlag_ReversePitch
        {
            get
            {
                return bacType.TrackingFlags.HasFlag(TrackingFlagsEnum.ReversePitch);
            }
            set
            {
                TrackingFlagsEnum flags = bacType.TrackingFlags.SetFlag(TrackingFlagsEnum.ReversePitch, value);

                if (flags != bacType.TrackingFlags)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type5>(nameof(bacType.TrackingFlags), bacType, bacType.TrackingFlags, flags, "TrackingFlag"));
                    bacType.TrackingFlags = flags;
                    RaisePropertyChanged(() => TrackingFlag_ReversePitch);
                }
            }
        }


        public BACType5ViewModel(BAC_Type5 _bacType)
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

        private void Instance_UndoOrRedoCalled(object sender, System.EventArgs e)
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
            RaisePropertyChanged(() => Tracking);
            RaisePropertyChanged(() => TrackingFlag_AimAtLockedTarget);
            RaisePropertyChanged(() => TrackingFlag_NoYaw);
            RaisePropertyChanged(() => TrackingFlag_Unk10);
            RaisePropertyChanged(() => TrackingFlag_Unk12);
            RaisePropertyChanged(() => TrackingFlag_ReverseYaw);
            RaisePropertyChanged(() => TrackingFlag_ReversePitch);
            RaisePropertyChanged(() => TrackingFlag_TrackForwardAndBackward);
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }

    }
}
