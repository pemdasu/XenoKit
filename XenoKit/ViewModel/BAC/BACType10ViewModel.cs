using GalaSoft.MvvmLight;
using System;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type10;

namespace XenoKit.ViewModel.BAC
{
    public partial class BACType10ViewModel : ObservableObject, IDisposable
    {

        private BAC_Type10 bacType;

        public string EanTypeLabel => Xv2CoreLib.ValuesDictionary.BAC.GetCameraEanTypeName(EanType);

        public ushort EanType
        {
            get
            {
                return (ushort)bacType.EanType;
            }
            set
            {
                if ((ushort)bacType.EanType == value) return;
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.EanType), bacType, bacType.EanType, (BAC_Type10.EanTypeEnum)value, "Camera EanType"));
                bacType.EanType = (BAC_Type10.EanTypeEnum)value;
                RaisePropertyChanged(() => EanType);
                RaisePropertyChanged(() => EanTypeLabel);
                RaisePropertyChanged(() => SpecifiedEan);
                RaisePropertyChanged(() => UseEanList);
                RaisePropertyChanged(() => EanIndex);
                bacType.RefreshType();
                UpdateBacPlayer();

            }
        }
        public ushort EanIndex
        {
            get
            {
                return bacType.EanIndex;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.EanIndex), bacType, bacType.EanIndex, value, "Camera Ean Index"));
                bacType.EanIndex = value;
                RaisePropertyChanged(() => EanIndex);
                UpdateBacPlayer();
                bacType.RefreshType();
            }
        }
        public BoneLinks BoneToFocusOn
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                if (bacType.BoneLink != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "BoneToFocusOn"));
                    bacType.BoneLink = value;
                    RaisePropertyChanged(() => BoneToFocusOn);
                    UpdateBacPlayer();
                }
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
                if (bacType.StartFrame != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.StartFrame), bacType, bacType.StartFrame, value, "Camera StartFrame"));
                    bacType.StartFrame = value;
                    RaisePropertyChanged(() => StartFrame);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort I_16
        {
            get
            {
                return bacType.I_16;
            }
            set
            {
                if (bacType.I_16 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.I_16), bacType, bacType.I_16, value, "Camera I_16"));
                    bacType.I_16 = value;
                    RaisePropertyChanged(() => I_16);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort GlobalModiferDuration
        {
            get
            {
                return bacType.GlobalModiferDuration;
            }
            set
            {
                if (bacType.GlobalModiferDuration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.GlobalModiferDuration), bacType, bacType.GlobalModiferDuration, value, "Camera GlobalModiferDuration"));
                    bacType.GlobalModiferDuration = value;
                    RaisePropertyChanged(() => GlobalModiferDuration);
                    UpdateBacPlayer();
                }
            }
        }
        public float PositionX
        {
            get
            {
                return bacType.PositionX;
            }
            set
            {
                if (bacType.PositionX != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionX), bacType, bacType.PositionX, value, "Camera PositionX"));
                    bacType.PositionX = value;
                    RaisePropertyChanged(() => PositionX);
                    UpdateBacPlayer();
                }
            }
        }
        public float PositionY
        {
            get
            {
                return bacType.PositionY;
            }
            set
            {
                if (bacType.PositionY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionY), bacType, bacType.PositionY, value, "Camera PositionY"));
                    bacType.PositionY = value;
                    RaisePropertyChanged(() => PositionY);
                    UpdateBacPlayer();
                }
            }
        }
        public float PositionZ
        {
            get
            {
                return bacType.PositionZ;
            }
            set
            {
                if (bacType.PositionZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionZ), bacType, bacType.PositionZ, value, "Camera PositionZ"));
                    bacType.PositionZ = value;
                    RaisePropertyChanged(() => PositionZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationX
        {
            get
            {
                return bacType.RotationX;
            }
            set
            {
                if (bacType.RotationX != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationX), bacType, bacType.RotationX, value, "Camera RotationX"));
                    bacType.RotationX = value;
                    RaisePropertyChanged(() => RotationX);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationY
        {
            get
            {
                return bacType.RotationY;
            }
            set
            {
                if (bacType.RotationY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationY), bacType, bacType.RotationY, value, "Camera RotationY"));
                    bacType.RotationY = value;
                    RaisePropertyChanged(() => RotationY);
                    UpdateBacPlayer();
                }
            }
        }
        public float RotationZ
        {
            get
            {
                return bacType.RotationZ;
            }
            set
            {
                if (bacType.RotationZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationZ), bacType, bacType.RotationZ, value, "Camera RotationZ"));
                    bacType.RotationZ = value;
                    RaisePropertyChanged(() => RotationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float DisplacementXZ
        {
            get
            {
                return bacType.DisplacementXZ;
            }
            set
            {
                if (bacType.DisplacementXZ != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DisplacementXZ), bacType, bacType.DisplacementXZ, value, "Camera DisplacementXZ"));
                    bacType.DisplacementXZ = value;
                    RaisePropertyChanged(() => DisplacementXZ);
                    UpdateBacPlayer();
                }
            }
        }
        public float DisplacementZY
        {
            get
            {
                return bacType.DisplacementZY;
            }
            set
            {
                if (bacType.DisplacementZY != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DisplacementZY), bacType, bacType.DisplacementZY, value, "Camera DisplacementZY"));
                    bacType.DisplacementZY = value;
                    RaisePropertyChanged(() => DisplacementZY);
                    UpdateBacPlayer();
                }
            }
        }
        public float FieldOfView
        {
            get
            {
                return bacType.FieldOfView;
            }
            set
            {
                if (bacType.FieldOfView != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.FieldOfView), bacType, bacType.FieldOfView, value, "Camera FieldOfView"));
                    bacType.FieldOfView = value;
                    RaisePropertyChanged(() => FieldOfView);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationX
        {
            get
            {
                return bacType.PositionX_Duration;
            }
            set
            {
                if (bacType.PositionX_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionX_Duration), bacType, bacType.PositionX_Duration, value, "Camera PositionDurationX"));
                    bacType.PositionX_Duration = value;
                    RaisePropertyChanged(() => PositionDurationX);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationY
        {
            get
            {
                return bacType.PositionY_Duration;
            }
            set
            {
                if (bacType.PositionY_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionY_Duration), bacType, bacType.PositionY_Duration, value, "Camera PositionDurationY"));
                    bacType.PositionY_Duration = value;
                    RaisePropertyChanged(() => PositionDurationY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort PositionDurationZ
        {
            get
            {
                return bacType.PositionZ_Duration;
            }
            set
            {
                if (bacType.PositionZ_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.PositionZ_Duration), bacType, bacType.PositionZ_Duration, value, "Camera PositionDurationZ"));
                    bacType.PositionZ_Duration = value;
                    RaisePropertyChanged(() => PositionDurationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationX
        {
            get
            {
                return bacType.RotationX_Duration;
            }
            set
            {
                if (bacType.RotationX_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationX_Duration), bacType, bacType.RotationX_Duration, value, "Camera RotationDurationX"));
                    bacType.RotationX_Duration = value;
                    RaisePropertyChanged(() => RotationDurationX);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationY
        {
            get
            {
                return bacType.RotationY_Duration;
            }
            set
            {
                if (bacType.RotationY_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationY_Duration), bacType, bacType.RotationY_Duration, value, "Camera RotationDurationY"));
                    bacType.RotationY_Duration = value;
                    RaisePropertyChanged(() => RotationDurationY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort RotationDurationZ
        {
            get
            {
                return bacType.RotationZ_Duration;
            }
            set
            {
                if (bacType.RotationZ_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.RotationZ_Duration), bacType, bacType.RotationZ_Duration, value, "Camera RotationDurationZ"));
                    bacType.RotationZ_Duration = value;
                    RaisePropertyChanged(() => RotationDurationZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort DisplacementDurationXZ
        {
            get
            {
                return bacType.DisplacementXZ_Duration;
            }
            set
            {
                if (bacType.DisplacementXZ_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DisplacementXZ_Duration), bacType, bacType.DisplacementXZ_Duration, value, "Camera DisplacementDurationXZ"));
                    bacType.DisplacementXZ_Duration = value;
                    RaisePropertyChanged(() => DisplacementDurationXZ);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort DisplacementDurationZY
        {
            get
            {
                return bacType.DisplacementZY_Duration;
            }
            set
            {
                if (bacType.DisplacementZY_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DisplacementZY_Duration), bacType, bacType.DisplacementZY_Duration, value, "Camera DisplacementDurationZY"));
                    bacType.DisplacementZY_Duration = value;
                    RaisePropertyChanged(() => DisplacementDurationZY);
                    UpdateBacPlayer();
                }
            }
        }
        public ushort FieldOfViewDuration
        {
            get
            {
                return bacType.FieldOfView_Duration;
            }
            set
            {
                if (bacType.FieldOfView_Duration != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.FieldOfView_Duration), bacType, bacType.FieldOfView_Duration, value, "Camera FieldOfViewDuration"));
                    bacType.FieldOfView_Duration = value;
                    RaisePropertyChanged(() => FieldOfViewDuration);
                    UpdateBacPlayer();
                }
            }
        }


        //Ean
        public EAN_File SpecifiedEan
        {
            get
            {
                if ((BAC_Type10.EanTypeEnum)EanType == BAC_Type10.EanTypeEnum.Skill && Files.Instance.SelectedMove.MoveType != Move.Type.Skill) return null;
                return Files.Instance.GetCamEanFile((BAC_Type10.EanTypeEnum)EanType, Files.Instance.SelectedMove, SceneManager.Actors[0], false, true);
            }
        }
        public bool UseEanList => SpecifiedEan != null;

        public BACType10ViewModel(BAC_Type10 _bacType)
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
            if (e.PropertyName == nameof(bacType.EanType)) RaisePropertyChanged(() => EanTypeLabel);
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => EanType);
            RaisePropertyChanged(() => EanTypeLabel);
            RaisePropertyChanged(() => EanIndex);
            RaisePropertyChanged(() => StartFrame);
            RaisePropertyChanged(() => BoneToFocusOn);
            RaisePropertyChanged(() => I_16);
            RaisePropertyChanged(() => GlobalModiferDuration);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => RotationX);
            RaisePropertyChanged(() => RotationY);
            RaisePropertyChanged(() => RotationZ);
            RaisePropertyChanged(() => DisplacementXZ);
            RaisePropertyChanged(() => DisplacementZY);
            RaisePropertyChanged(() => FieldOfView);
            RaisePropertyChanged(() => PositionDurationX);
            RaisePropertyChanged(() => PositionDurationY);
            RaisePropertyChanged(() => PositionDurationZ);
            RaisePropertyChanged(() => RotationDurationX);
            RaisePropertyChanged(() => RotationDurationY);
            RaisePropertyChanged(() => RotationDurationZ);
            RaisePropertyChanged(() => DisplacementDurationXZ);
            RaisePropertyChanged(() => DisplacementDurationZY);
            RaisePropertyChanged(() => EnableTransformModifers);
            RaisePropertyChanged(() => EnableCameraForAllPlayers);
            RaisePropertyChanged(() => ResetCameraAfterEnd);
            RaisePropertyChanged(() => FocusOnTarget);
            RaisePropertyChanged(() => DontFitToBodySize);
            RaisePropertyChanged(() => Flag_74_4);
            RaisePropertyChanged(() => KeepWhileInputHeld);
            RaisePropertyChanged(() => RequireTenHitCombo);
            RaisePropertyChanged(() => SpecifiedEan);
            RaisePropertyChanged(() => UseEanList);

            RaisePropertyChanged(() => LockCameraPriority);
            RaisePropertyChanged(() => CameraFlags_Unk10);
            RaisePropertyChanged(() => CameraFlags_Unk11);
            RaisePropertyChanged(() => CameraFlags_KeepAfterEnd);
            RaisePropertyChanged(() => CameraFlags_UseType26Row);
            RaisePropertyChanged(() => DelayCameraBlend);
            RaisePropertyChanged(() => ReverseCameraDirection);
            RaisePropertyChanged(() => ResumeFromEndView);

            UpdateBacPlayer();
        }


        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }
    }
}
