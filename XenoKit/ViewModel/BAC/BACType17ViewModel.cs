using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type17;

namespace XenoKit.ViewModel.BAC
{
    public class BACType17ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type17 bacType;

        public BoneLinks UserBone
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "ThrowHandler UserBone"));
                bacType.BoneLink = value;
                RaisePropertyChanged(() => UserBone);
            }
        }
        public BoneLinks VictimBone
        {
            get
            {
                return bacType.VictimBone;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.VictimBone), bacType, bacType.VictimBone, value, "ThrowHandler VictimBone"));
                bacType.VictimBone = value;
                RaisePropertyChanged(() => VictimBone);
            }
        }
        public ushort BacEntryID
        {
            get
            {
                return bacType.BacEntryId;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.BacEntryId), bacType, bacType.BacEntryId, value, "ThrowHandler BacEntryId"));
                bacType.BacEntryId = value;
                RaisePropertyChanged(() => BacEntryID);
            }
        }
        public bool Flag2_CompareSizeNoBlend
        {
            get
            {
                return bacType.ThrowHandlerFlags2.HasFlag(BAC_Type17.ThrowHandlerFlags2Enum.CompareSizeNoBlend);
            }
            set
            {
                SetThrowHandlerFlags2(BAC_Type17.ThrowHandlerFlags2Enum.CompareSizeNoBlend, value);
                RaisePropertyChanged(() => Flag2_CompareSizeNoBlend);
            }
        }
        public bool Flag2_CompareSizeBlend
        {
            get
            {
                return bacType.ThrowHandlerFlags2.HasFlag(BAC_Type17.ThrowHandlerFlags2Enum.CompareSizeBlend);
            }
            set
            {
                SetThrowHandlerFlags2(BAC_Type17.ThrowHandlerFlags2Enum.CompareSizeBlend, value);
                RaisePropertyChanged(() => Flag2_CompareSizeBlend);
            }
        }
        public bool Flag2_ThrowerMoves
        {
            get
            {
                return bacType.ThrowHandlerFlags2.HasFlag(BAC_Type17.ThrowHandlerFlags2Enum.ThrowerMoves);
            }
            set
            {
                SetThrowHandlerFlags2(BAC_Type17.ThrowHandlerFlags2Enum.ThrowerMoves, value);
                RaisePropertyChanged(() => Flag2_ThrowerMoves);
            }
        }
        public bool Flag2_KeepOutOfGeometry
        {
            get
            {
                return bacType.ThrowHandlerFlags2.HasFlag(BAC_Type17.ThrowHandlerFlags2Enum.KeepOutOfGeometry);
            }
            set
            {
                SetThrowHandlerFlags2(BAC_Type17.ThrowHandlerFlags2Enum.KeepOutOfGeometry, value);
                RaisePropertyChanged(() => Flag2_KeepOutOfGeometry);
            }
        }
        public bool Flag2_UseBodyRotation
        {
            get
            {
                return bacType.ThrowHandlerFlags2.HasFlag(BAC_Type17.ThrowHandlerFlags2Enum.UseBodyRotation);
            }
            set
            {
                SetThrowHandlerFlags2(BAC_Type17.ThrowHandlerFlags2Enum.UseBodyRotation, value);
                RaisePropertyChanged(() => Flag2_UseBodyRotation);
            }
        }
        public bool Flag2_Unk6
        {
            get
            {
                return bacType.ThrowHandlerFlags2.HasFlag(BAC_Type17.ThrowHandlerFlags2Enum.Unk6);
            }
            set
            {
                SetThrowHandlerFlags2(BAC_Type17.ThrowHandlerFlags2Enum.Unk6, value);
                RaisePropertyChanged(() => Flag2_Unk6);
            }
        }
        public IReadOnlyDictionary<ushort, string> FollowStates { get; } = new Dictionary<ushort, string>
        {
            { 0, "Neutral" },
            { 1, "Bounce" },
            { 2, "Down" }
        };

        public string FollowStateLabel => FollowStates.TryGetValue(FollowState, out string name)
            ? name : $"Unknown ({FollowState})";

        public ushort FollowState
        {
            get
            {
                return bacType.FollowState;
            }
            set
            {
                if (bacType.FollowState == value) return;
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.FollowState), bacType, bacType.FollowState, value, "ThrowHandler FollowState"));
                bacType.FollowState = value;
                RaisePropertyChanged(() => FollowState);
                RaisePropertyChanged(() => FollowStateLabel);
            }
        }
        public float DisplacementX
        {
            get
            {
                return bacType.DisplacementX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.DisplacementX), bacType, bacType.DisplacementX, value, "ThrowHandler DisplacementX"));
                bacType.DisplacementX = value;
                RaisePropertyChanged(() => DisplacementX);
            }
        }
        public float DisplacementY
        {
            get
            {
                return bacType.DisplacementY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.DisplacementY), bacType, bacType.DisplacementY, value, "ThrowHandler DisplacementY"));
                bacType.DisplacementY = value;
                RaisePropertyChanged(() => DisplacementY);
            }
        }
        public float DisplacementZ
        {
            get
            {
                return bacType.DisplacementZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(bacType.DisplacementZ), bacType, bacType.DisplacementZ, value, "ThrowHandler DisplacementZ"));
                bacType.DisplacementZ = value;
                RaisePropertyChanged(() => DisplacementZ);
            }
        }


        //Flags
        public bool Flag_UseSlot0
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.UseSlot0);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.UseSlot0, value);
                RaisePropertyChanged(() => Flag_UseSlot0);
            }
        }
        public bool Flag_UseSlot1
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.UseSlot1);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.UseSlot1, value);
                RaisePropertyChanged(() => Flag_UseSlot1);
            }
        }
        public bool Flag_TurnAroundOnJump
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.TurnAroundOnJump);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.TurnAroundOnJump, value);
                RaisePropertyChanged(() => Flag_TurnAroundOnJump);
            }
        }
        public bool Flag_FaceAwayOnJump
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.FaceAwayOnJump);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.FaceAwayOnJump, value);
                RaisePropertyChanged(() => Flag_FaceAwayOnJump);
            }
        }
        public bool Flag_FaceTowardOnJump
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk5);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk5, value);
                RaisePropertyChanged(() => Flag_FaceTowardOnJump);
            }
        }
        public bool Flag_UpdateAnimationOnJump
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.BacJump_AfterDuration);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.BacJump_AfterDuration, value);
                RaisePropertyChanged(() => Flag_UpdateAnimationOnJump);
            }
        }
        public bool Flag_Unk7
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk7);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk7, value);
                RaisePropertyChanged(() => Flag_Unk7);
            }
        }
        public bool Flag_JumpWhenAnyLands
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.JumpWhenAnyLands);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.JumpWhenAnyLands, value);
                RaisePropertyChanged(() => Flag_JumpWhenAnyLands);
            }
        }
        public bool Flag_JumpWhenSelfLands
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.JumpWhenSelfLands);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.JumpWhenSelfLands, value);
                RaisePropertyChanged(() => Flag_JumpWhenSelfLands);
            }
        }
        public bool Flag_JumpWhenAnimationEnds
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.JumpWhenAnimationEnds);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.JumpWhenAnimationEnds, value);
                RaisePropertyChanged(() => Flag_JumpWhenAnimationEnds);
            }
        }
        public bool Flag_SnapToBind
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.SnapToBind);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.SnapToBind, value);
                RaisePropertyChanged(() => Flag_SnapToBind);
            }
        }
        public bool Flag_Unk12
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk12);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk12, value);
                RaisePropertyChanged(() => Flag_Unk12);
            }
        }
        public bool Flag_KeepPitch
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.KeepPitch);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.KeepPitch, value);
                RaisePropertyChanged(() => Flag_KeepPitch);
            }
        }
        public bool Flag_Unk14
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk14);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk14, value);
                RaisePropertyChanged(() => Flag_Unk14);
            }
        }
        public bool Flag_Unk15
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk15);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk15, value);
                RaisePropertyChanged(() => Flag_Unk15);
            }
        }
        public bool Flag_Unk16
        {
            get
            {
                return bacType.ThrowHandlerFlags.HasFlag(ThrowHandlerFlagsEnum.Unk16);
            }
            set
            {
                SetThrowHandlerFlags(ThrowHandlerFlagsEnum.Unk16, value);
                RaisePropertyChanged(() => Flag_Unk16);
            }
        }



        public BACType17ViewModel(BAC_Type17 _bacType)
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
            if (e.PropertyName == nameof(bacType.FollowState)) RaisePropertyChanged(() => FollowStateLabel);
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => UserBone);
            RaisePropertyChanged(() => VictimBone);
            RaisePropertyChanged(() => BacEntryID);
            RaisePropertyChanged(() => Flag2_CompareSizeNoBlend);
            RaisePropertyChanged(() => Flag2_CompareSizeBlend);
            RaisePropertyChanged(() => Flag2_ThrowerMoves);
            RaisePropertyChanged(() => Flag2_KeepOutOfGeometry);
            RaisePropertyChanged(() => Flag2_UseBodyRotation);
            RaisePropertyChanged(() => Flag2_Unk6);
            RaisePropertyChanged(() => FollowState);
            RaisePropertyChanged(() => FollowStateLabel);
            RaisePropertyChanged(() => DisplacementX);
            RaisePropertyChanged(() => DisplacementY);
            RaisePropertyChanged(() => DisplacementZ);

            RaisePropertyChanged(() => Flag_UseSlot0);
            RaisePropertyChanged(() => Flag_UseSlot1);
            RaisePropertyChanged(() => Flag_TurnAroundOnJump);
            RaisePropertyChanged(() => Flag_FaceAwayOnJump);
            RaisePropertyChanged(() => Flag_FaceTowardOnJump);
            RaisePropertyChanged(() => Flag_UpdateAnimationOnJump);
            RaisePropertyChanged(() => Flag_Unk7);
            RaisePropertyChanged(() => Flag_JumpWhenAnyLands);
            RaisePropertyChanged(() => Flag_JumpWhenSelfLands);
            RaisePropertyChanged(() => Flag_JumpWhenAnimationEnds);
            RaisePropertyChanged(() => Flag_SnapToBind);
            RaisePropertyChanged(() => Flag_Unk12);
            RaisePropertyChanged(() => Flag_KeepPitch);
            RaisePropertyChanged(() => Flag_Unk14);
            RaisePropertyChanged(() => Flag_Unk15);
            RaisePropertyChanged(() => Flag_Unk16);
        }

        private void SetThrowHandlerFlags2(BAC_Type17.ThrowHandlerFlags2Enum flag, bool state)
        {
            var newFlag = bacType.ThrowHandlerFlags2.SetFlag(flag, state);

            if (bacType.ThrowHandlerFlags2 != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(BAC_Type17.ThrowHandlerFlags2), bacType, bacType.ThrowHandlerFlags2, newFlag, "ThrowHandlerFlags2"));
                bacType.ThrowHandlerFlags2 = newFlag;
            }
        }

        private void SetThrowHandlerFlags(ThrowHandlerFlagsEnum flag, bool state)
        {
            var newFlag = bacType.ThrowHandlerFlags.SetFlag(flag, state);

            if (bacType.ThrowHandlerFlags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type17>(nameof(BAC_Type17.ThrowHandlerFlags), bacType, bacType.ThrowHandlerFlags, newFlag, "ThrowHandlerFlags"));
                bacType.ThrowHandlerFlags = newFlag;
            }
        }
    }
}
