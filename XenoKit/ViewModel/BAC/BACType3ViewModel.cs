using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType3ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type3 bacType;

        public IReadOnlyDictionary<ushort, string> HurtboxParts { get; } = new Dictionary<ushort, string>
        {
            { 0, "All parts" },
            { 0x8, "Right elbow" },
            { 0x10, "Left elbow" },
            { 0x20, "Right foot" },
            { 0x40, "Left foot" },
            { 0x1, "Head / Neck" },
            { 0x2, "Back" },
            { 0x4, "Front" },
            { 0x80, "Tail" },
            { 0x200, "Right wing" },
            { 0x400, "Left wing" },
            { 0x100, "Head region 2" },
            { 0x18, "Both elbows" },
            { 0x60, "Both feet" },
            { 0x600, "Both wings" },
            { 0xff, "Head, torso, limbs and tail" }
        };

        public string HurtboxPartsLabel => HurtboxParts.TryGetValue(HurtboxPartMask, out string name)
            ? name : $"Unknown ({HurtboxPartMask})";

        public ushort Type
        {
            get
            {
                return bacType.InvulnerabilityType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type3>(nameof(bacType.InvulnerabilityType), bacType, bacType.InvulnerabilityType, value, "Invulnerability Type"));
                bacType.InvulnerabilityType = value;
                RaisePropertyChanged(() => Type);
                bacType.RefreshType();
            }
        }
        public ushort HurtboxPartMask
        {
            get
            {
                return bacType.HurtboxPartMask;
            }
            set
            {
                if (bacType.HurtboxPartMask == value) return;
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type3>(nameof(bacType.HurtboxPartMask), bacType, bacType.HurtboxPartMask, value, "Invulnerability Hurtbox Part Mask"));
                bacType.HurtboxPartMask = value;
                UpdateHurtboxParts();
            }
        }


        public BACType3ViewModel(BAC_Type3 _bacType)
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
            if (e.PropertyName == nameof(bacType.HurtboxPartMask)) UpdateHurtboxParts();
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => Type);
            UpdateHurtboxParts();
        }

        private void UpdateHurtboxParts()
        {
            RaisePropertyChanged(() => HurtboxPartMask);
            RaisePropertyChanged(() => HurtboxPartsLabel);
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }

    }
}
