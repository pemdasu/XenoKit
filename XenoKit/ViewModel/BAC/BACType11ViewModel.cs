using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType11ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type11 bacType;

        public ushort AcbType
        {
            get
            {
                return (ushort)bacType.AcbType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type11>(nameof(bacType.AcbType), bacType, bacType.AcbType, (AcbType)value, "Sound AcbType"));
                bacType.AcbType = (AcbType)value;
                RaisePropertyChanged(() => AcbType);
                RaisePropertyChanged(() => AcbToolTip);
                RaisePropertyChanged(() => CueId);
                bacType.RefreshType();
            }
        }
        public string AcbToolTip => AcbType == (ushort)Xv2CoreLib.BAC.AcbType.Footsteps
            ? "Uses CAR_BTL_BKG_CMN.acb in data/sound/SE/Battle/Field. Preview uses ground material 1."
            : AcbType == (ushort)Xv2CoreLib.BAC.AcbType.Mission_SE
                ? "Uses CAR_Mission_DB_SE.acb in data/sound/SE/Battle/Mission."
                : null;

        public ushort CueId
        {
            get
            {
                return bacType.CueId;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type11>(nameof(bacType.CueId), bacType, bacType.CueId, value, "Sound CueId"));
                bacType.CueId = value;
                RaisePropertyChanged(() => CueId);
                bacType.RefreshType();
            }
        }
        

        //Flags
        public bool Flag_Unk1
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk1);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk1, value);
                RaisePropertyChanged(() => Flag_Unk1);
            }
        }
        public bool Flag_FadeAtRowEnd
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.FadeAtRowEnd);
            }
            set
            {
                SetSoundFlags(SoundFlags.FadeAtRowEnd, value);
                RaisePropertyChanged(() => Flag_FadeAtRowEnd);
            }
        }
        public bool Flag_RandomPickSameStart
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.RandomPickSameStart);
            }
            set
            {
                SetSoundFlags(SoundFlags.RandomPickSameStart, value);
                RaisePropertyChanged(() => Flag_RandomPickSameStart);
            }
        }
        public bool Flag_StopWhenParentEnds
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.StopWhenParentEnds);
            }
            set
            {
                SetSoundFlags(SoundFlags.StopWhenParentEnds, value);
                RaisePropertyChanged(() => Flag_StopWhenParentEnds);
            }
        }
        public bool Flag_Unk5
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk5);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk5, value);
                RaisePropertyChanged(() => Flag_Unk5);
            }
        }
        public bool Flag_Unk6
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk6);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk6, value);
                RaisePropertyChanged(() => Flag_Unk6);
            }
        }
        public bool Flag_FadeOnStateChange
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.FadeOnStateChange);
            }
            set
            {
                SetSoundFlags(SoundFlags.FadeOnStateChange, value);
                RaisePropertyChanged(() => Flag_FadeOnStateChange);
            }
        }
        public bool Flag_Unk8
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk8);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk8, value);
                RaisePropertyChanged(() => Flag_Unk8);
            }
        }
        public bool Flag_Mark
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Mark);
            }
            set
            {
                SetSoundFlags(SoundFlags.Mark, value);
                RaisePropertyChanged(() => Flag_Mark);
            }
        }
        public bool Flag_FadeAllMarked
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.FadeAllMarked);
            }
            set
            {
                SetSoundFlags(SoundFlags.FadeAllMarked, value);
                RaisePropertyChanged(() => Flag_FadeAllMarked);
            }
        }
        public bool Flag_FadeMarked
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.FadeMarked);
            }
            set
            {
                SetSoundFlags(SoundFlags.FadeMarked, value);
                RaisePropertyChanged(() => Flag_FadeMarked);
            }
        }
        public bool Flag_FadeOnStateChangeExceptState12
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.FadeOnStateChangeExceptState12);
            }
            set
            {
                SetSoundFlags(SoundFlags.FadeOnStateChangeExceptState12, value);
                RaisePropertyChanged(() => Flag_FadeOnStateChangeExceptState12);
            }
        }
        public bool Flag_Unk13
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk13);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk13, value);
                RaisePropertyChanged(() => Flag_Unk13);
            }
        }
        public bool Flag_Unk14
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk14);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk14, value);
                RaisePropertyChanged(() => Flag_Unk14);
            }
        }
        public bool Flag_Unk15
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk15);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk15, value);
                RaisePropertyChanged(() => Flag_Unk15);
            }
        }
        public bool Flag_Unk16
        {
            get
            {
                return bacType.SoundFlags.HasFlag(SoundFlags.Unk16);
            }
            set
            {
                SetSoundFlags(SoundFlags.Unk16, value);
                RaisePropertyChanged(() => Flag_Unk16);
            }
        }

        public BACType11ViewModel(BAC_Type11 _bacType)
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
            RaisePropertyChanged(() => AcbType);
            RaisePropertyChanged(() => AcbToolTip);
            RaisePropertyChanged(() => CueId);
            RaisePropertyChanged(() => Flag_Unk1);
            RaisePropertyChanged(() => Flag_FadeAtRowEnd);
            RaisePropertyChanged(() => Flag_RandomPickSameStart);
            RaisePropertyChanged(() => Flag_StopWhenParentEnds);
            RaisePropertyChanged(() => Flag_Unk5);
            RaisePropertyChanged(() => Flag_Unk6);
            RaisePropertyChanged(() => Flag_FadeOnStateChange);
            RaisePropertyChanged(() => Flag_Unk8);
            RaisePropertyChanged(() => Flag_Mark);
            RaisePropertyChanged(() => Flag_FadeAllMarked);
            RaisePropertyChanged(() => Flag_FadeMarked);
            RaisePropertyChanged(() => Flag_FadeOnStateChangeExceptState12);
            RaisePropertyChanged(() => Flag_Unk13);
            RaisePropertyChanged(() => Flag_Unk14);
            RaisePropertyChanged(() => Flag_Unk15);
            RaisePropertyChanged(() => Flag_Unk16);
        }

        private void SetSoundFlags(SoundFlags flag, bool state)
        {
            var newFlag = bacType.SoundFlags.SetFlag(flag, state);

            if (bacType.SoundFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type11>(nameof(BAC_Type11.SoundFlags), bacType, bacType.SoundFlags, newFlag, "SoundFlags"));
                bacType.SoundFlags = newFlag;
            }
        }
    }
}
