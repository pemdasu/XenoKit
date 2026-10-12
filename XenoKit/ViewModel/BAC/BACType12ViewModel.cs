using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType12ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type12 bacType;
        
        public bool Flag_Unk1
        {
            get { return bacType.Axis.HasFlag(TargetingAxis.Unk1); }
            set { SetFlag(TargetingAxis.Unk1, value); }
        }
        public bool Flag_PitchTracking
        {
            get { return bacType.Axis.HasFlag(TargetingAxis.PitchTrackingEnable); }
            set { SetFlag(TargetingAxis.PitchTrackingEnable, value); }
        }
        public bool Flag_Unk3
        {
            get { return bacType.Axis.HasFlag(TargetingAxis.Unk3); }
            set { SetFlag(TargetingAxis.Unk3, value); }
        }
        public bool Flag_Unk4
        {
            get { return bacType.Axis.HasFlag(TargetingAxis.Unk4); }
            set { SetFlag(TargetingAxis.Unk4, value); }
        }

        private void SetFlag(TargetingAxis flag, bool value)
        {
            TargetingAxis newFlags = bacType.Axis.SetFlag(flag, value);

            if (newFlags == bacType.Axis)
                return;

            UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type12>(nameof(bacType.Axis), bacType, bacType.Axis, newFlags, "Tracking Axis"));
            bacType.Axis = newFlags;
            UpdateProperties();
            bacType.RefreshType();
        }


        public BACType12ViewModel(BAC_Type12 _bacType)
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
            bacType.RefreshType();
        }

        private void BacType_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => Flag_Unk1);
            RaisePropertyChanged(() => Flag_PitchTracking);
            RaisePropertyChanged(() => Flag_Unk3);
            RaisePropertyChanged(() => Flag_Unk4);
        }
    }
}
