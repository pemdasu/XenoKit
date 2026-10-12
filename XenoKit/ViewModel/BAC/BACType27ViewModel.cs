using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type8;

namespace XenoKit.ViewModel.BAC
{
    public class BACType27ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type27 bacType;

        public ushort SkillID
        {
            get
            {
                return bacType.SkillID;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type27>(nameof(bacType.SkillID), bacType, bacType.SkillID, value, "SkillID"));
                bacType.SkillID = value;
                RaisePropertyChanged(() => SkillID);
            }
        }
        public ushort SkillType
        {
            get
            {
                return (ushort)bacType.SkillType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type27>(nameof(bacType.SkillType), bacType, bacType.SkillType, (EepkTypeEnum)value, "SkillType"));
                bacType.SkillType = (EepkTypeEnum)value;
                RaisePropertyChanged(() => SkillType);
                bacType.RefreshType();
            }
        }
        public ushort EffectID
        {
            get
            {
                return bacType.EffectID;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type27>(nameof(bacType.EffectID), bacType, bacType.EffectID, value, "EffectID"));
                bacType.EffectID = value;
                RaisePropertyChanged(() => EffectID);
            }
        }
        public ushort FunctionDuration
        {
            get
            {
                return bacType.FunctionDuration;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type27>(nameof(bacType.FunctionDuration), bacType, bacType.FunctionDuration, value, "FunctionDuration"));
                bacType.FunctionDuration = value;
                RaisePropertyChanged(() => FunctionDuration);
            }
        }
        public bool Function_UseMaskAndDuration
        {
            get
            {
                return (bacType.FunctionFlags & 0x1) != 0;
            }
            set
            {
                SetFunctionFlag(0x1, value);
                RaisePropertyChanged(() => Function_UseMaskAndDuration);
            }
        }
        public bool Function_Mask1
        {
            get
            {
                return (bacType.FunctionFlags & 0x4) != 0;
            }
            set
            {
                SetFunctionFlag(0x4, value);
                RaisePropertyChanged(() => Function_Mask1);
            }
        }
        public bool Function_Mask2
        {
            get
            {
                return (bacType.FunctionFlags & 0x8) != 0;
            }
            set
            {
                SetFunctionFlag(0x8, value);
                RaisePropertyChanged(() => Function_Mask2);
            }
        }
        public bool Function_Mask4
        {
            get
            {
                return (bacType.FunctionFlags & 0x10) != 0;
            }
            set
            {
                SetFunctionFlag(0x10, value);
                RaisePropertyChanged(() => Function_Mask4);
            }
        }
        private void SetFunctionFlag(ushort flag, bool state)
        {
            ushort newFlags = (ushort)(state ? (bacType.FunctionFlags | flag) : (bacType.FunctionFlags & ~flag));

            if (newFlags != bacType.FunctionFlags)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type27>(nameof(bacType.FunctionFlags), bacType, bacType.FunctionFlags, newFlags, "Function Flags"));
                bacType.FunctionFlags = newFlags;
            }
        }
        public ushort I_18
        {
            get
            {
                return bacType.I_18;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type27>(nameof(bacType.I_18), bacType, bacType.I_18, value, "I_18"));
                bacType.I_18 = value;
                RaisePropertyChanged(() => I_18);
            }
        }
        public ushort I_20
        {
            get
            {
                return bacType.I_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type27>(nameof(bacType.I_20), bacType, bacType.I_20, value, "I_20"));
                bacType.I_20 = value;
                RaisePropertyChanged(() => I_20);
            }
        }
        public ushort I_22
        {
            get
            {
                return bacType.I_22;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type27>(nameof(bacType.I_22), bacType, bacType.I_22, value, "I_22"));
                bacType.I_22 = value;
                RaisePropertyChanged(() => I_22);
            }
        }


        public BACType27ViewModel(BAC_Type27 _bacType)
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
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => SkillType);
            RaisePropertyChanged(() => EffectID);
            RaisePropertyChanged(() => FunctionDuration);
            RaisePropertyChanged(() => Function_UseMaskAndDuration);
            RaisePropertyChanged(() => Function_Mask1);
            RaisePropertyChanged(() => Function_Mask2);
            RaisePropertyChanged(() => Function_Mask4);
            RaisePropertyChanged(() => I_18);
            RaisePropertyChanged(() => I_20);
            RaisePropertyChanged(() => I_22);
        }


    }
}
