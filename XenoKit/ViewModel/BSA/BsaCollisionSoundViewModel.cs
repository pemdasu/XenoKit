using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaCollisionSoundViewModel : ObservableObject, IDisposable
    {
        private readonly BSA_CollisionSound collisionSound;

        public AcbType AcbType { get => collisionSound.AcbType; set => SetValue(nameof(collisionSound.AcbType), collisionSound.AcbType, value, v => collisionSound.AcbType = v, "BSA Collision Sound ACB Type"); }
        public ushort I_02 { get => collisionSound.I_02; set => SetValue(nameof(collisionSound.I_02), collisionSound.I_02, value, v => collisionSound.I_02 = v, "BSA Collision Sound I_02"); }
        public int CueId { get => collisionSound.CueId; set => SetValue(nameof(collisionSound.CueId), collisionSound.CueId, value, v => collisionSound.CueId = v, "BSA Collision Sound Cue ID"); }

        public BsaCollisionSoundViewModel(BSA_CollisionSound collisionSound)
        {
            this.collisionSound = collisionSound;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => AcbType);
            RaisePropertyChanged(() => I_02);
            RaisePropertyChanged(() => CueId);
        }

        private void SetValue<TValue>(string modelProperty, TValue oldValue, TValue newValue, Action<TValue> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_CollisionSound>(modelProperty, collisionSound, oldValue, newValue, undoName));
            assign(newValue);
            RaisePropertyChanged(viewModelProperty);
        }
    }
}
