using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType28ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type28 bacType;

        public ushort StartMainCluster
        {
            get => bacType.StartMainCluster;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.StartMainCluster), bacType, bacType.StartMainCluster, value, "StartMainCluster"));
                bacType.StartMainCluster = value;
                RaisePropertyChanged(() => StartMainCluster);
            }
        }
        public ushort StartBlendCluster
        {
            get => bacType.StartBlendCluster;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.StartBlendCluster), bacType, bacType.StartBlendCluster, value, "StartBlendCluster"));
                bacType.StartBlendCluster = value;
                RaisePropertyChanged(() => StartBlendCluster);
            }
        }
        public ushort EndMainCluster
        {
            get => bacType.EndMainCluster;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.EndMainCluster), bacType, bacType.EndMainCluster, value, "EndMainCluster"));
                bacType.EndMainCluster = value;
                RaisePropertyChanged(() => EndMainCluster);
            }
        }
        public ushort EndBlendCluster
        {
            get => bacType.EndBlendCluster;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.EndBlendCluster), bacType, bacType.EndBlendCluster, value, "EndBlendCluster"));
                bacType.EndBlendCluster = value;
                RaisePropertyChanged(() => EndBlendCluster);
            }
        }
        public float StartBlendRatio
        {
            get => bacType.StartBlendRatio;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.StartBlendRatio), bacType, bacType.StartBlendRatio, value, "StartBlendRatio"));
                bacType.StartBlendRatio = value;
                RaisePropertyChanged(() => StartBlendRatio);
            }
        }
        public float EndBlendRatio
        {
            get => bacType.EndBlendRatio;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.EndBlendRatio), bacType, bacType.EndBlendRatio, value, "EndBlendRatio"));
                bacType.EndBlendRatio = value;
                RaisePropertyChanged(() => EndBlendRatio);
            }
        }
        public int I_24
        {
            get => bacType.I_24;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.I_24), bacType, bacType.I_24, value, "I_24"));
                bacType.I_24 = value;
                RaisePropertyChanged(() => I_24);
            }
        }
        public int I_28
        {
            get => bacType.I_28;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.I_28), bacType, bacType.I_28, value, "I_28"));
                bacType.I_28 = value;
                RaisePropertyChanged(() => I_28);
            }
        }
        public int I_32
        {
            get => bacType.I_32;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type28>(nameof(bacType.I_32), bacType, bacType.I_32, value, "I_32"));
                bacType.I_32 = value;
                RaisePropertyChanged(() => I_32);
            }
        }

        public BACType28ViewModel(BAC_Type28 _bacType)
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
            RaisePropertyChanged(() => StartMainCluster);
            RaisePropertyChanged(() => StartBlendCluster);
            RaisePropertyChanged(() => EndMainCluster);
            RaisePropertyChanged(() => EndBlendCluster);
            RaisePropertyChanged(() => StartBlendRatio);
            RaisePropertyChanged(() => EndBlendRatio);
            RaisePropertyChanged(() => I_24);
            RaisePropertyChanged(() => I_28);
            RaisePropertyChanged(() => I_32);
        }


    }
}
