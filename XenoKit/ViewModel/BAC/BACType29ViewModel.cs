using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType29ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type29 bacType;

        public ushort ModelIndex
        {
            get => bacType.ModelIndex;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.ModelIndex), bacType, bacType.ModelIndex, value, "ModelIndex"));
                bacType.ModelIndex = value;
                RaisePropertyChanged(() => ModelIndex);
            }
        }
        public ushort MaterialIndex
        {
            get => bacType.MaterialIndex;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.MaterialIndex), bacType, bacType.MaterialIndex, value, "MaterialIndex"));
                bacType.MaterialIndex = value;
                RaisePropertyChanged(() => MaterialIndex);
            }
        }
        public int ShaderParamId
        {
            get => bacType.ShaderParamId;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.ShaderParamId), bacType, bacType.ShaderParamId, value, "ShaderParamId"));
                bacType.ShaderParamId = value;
                RaisePropertyChanged(() => ShaderParamId);
            }
        }
        public float StartValue1
        {
            get => bacType.StartValue1;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.StartValue1), bacType, bacType.StartValue1, value, "StartValue1"));
                bacType.StartValue1 = value;
                RaisePropertyChanged(() => StartValue1);
            }
        }
        public float StartValue2
        {
            get => bacType.StartValue2;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.StartValue2), bacType, bacType.StartValue2, value, "StartValue2"));
                bacType.StartValue2 = value;
                RaisePropertyChanged(() => StartValue2);
            }
        }
        public float StartValue3
        {
            get => bacType.StartValue3;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.StartValue3), bacType, bacType.StartValue3, value, "StartValue3"));
                bacType.StartValue3 = value;
                RaisePropertyChanged(() => StartValue3);
            }
        }
        public float StartValue4
        {
            get => bacType.StartValue4;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.StartValue4), bacType, bacType.StartValue4, value, "StartValue4"));
                bacType.StartValue4 = value;
                RaisePropertyChanged(() => StartValue4);
            }
        }
        public float EndValue1
        {
            get => bacType.EndValue1;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.EndValue1), bacType, bacType.EndValue1, value, "EndValue1"));
                bacType.EndValue1 = value;
                RaisePropertyChanged(() => EndValue1);
            }
        }
        public float EndValue2
        {
            get => bacType.EndValue2;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.EndValue2), bacType, bacType.EndValue2, value, "EndValue2"));
                bacType.EndValue2 = value;
                RaisePropertyChanged(() => EndValue2);
            }
        }
        public float EndValue3
        {
            get => bacType.EndValue3;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.EndValue3), bacType, bacType.EndValue3, value, "EndValue3"));
                bacType.EndValue3 = value;
                RaisePropertyChanged(() => EndValue3);
            }
        }
        public float EndValue4
        {
            get => bacType.EndValue4;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.EndValue4), bacType, bacType.EndValue4, value, "EndValue4"));
                bacType.EndValue4 = value;
                RaisePropertyChanged(() => EndValue4);
            }
        }
        public int I_48
        {
            get => bacType.I_48;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_48), bacType, bacType.I_48, value, "I_48"));
                bacType.I_48 = value;
                RaisePropertyChanged(() => I_48);
            }
        }
        public int I_52
        {
            get => bacType.I_52;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_52), bacType, bacType.I_52, value, "I_52"));
                bacType.I_52 = value;
                RaisePropertyChanged(() => I_52);
            }
        }
        public int I_56
        {
            get => bacType.I_56;
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type29>(nameof(bacType.I_56), bacType, bacType.I_56, value, "I_56"));
                bacType.I_56 = value;
                RaisePropertyChanged(() => I_56);
            }
        }


        public BACType29ViewModel(BAC_Type29 _bacType)
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
            RaisePropertyChanged(() => ModelIndex);
            RaisePropertyChanged(() => MaterialIndex);
            RaisePropertyChanged(() => ShaderParamId);
            RaisePropertyChanged(() => StartValue1);
            RaisePropertyChanged(() => StartValue2);
            RaisePropertyChanged(() => StartValue3);
            RaisePropertyChanged(() => StartValue4);
            RaisePropertyChanged(() => EndValue1);
            RaisePropertyChanged(() => EndValue2);
            RaisePropertyChanged(() => EndValue3);
            RaisePropertyChanged(() => EndValue4);
            RaisePropertyChanged(() => I_48);
            RaisePropertyChanged(() => I_52);
            RaisePropertyChanged(() => I_56);
        }


    }
}
