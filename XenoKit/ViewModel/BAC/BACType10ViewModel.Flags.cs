using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using static Xv2CoreLib.BAC.BAC_Type10;

namespace XenoKit.ViewModel.BAC
{
    public partial class BACType10ViewModel
    {
        public bool EnableTransformModifers
        {
            get
            {
                return bacType.EnableTransformModifiers;
            }
            set
            {
                if (bacType.EnableTransformModifiers != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.EnableTransformModifiers), bacType, bacType.EnableTransformModifiers, value, "Camera EnableTransformModifers"));
                    bacType.EnableTransformModifiers = value;
                    RaisePropertyChanged(() => EnableTransformModifers);
                    UpdateBacPlayer();
                }
            }
        }
        public bool EnableCameraForAllPlayers
        {
            get
            {
                return bacType.EnableCameraForAllPlayers;
            }
            set
            {
                if (bacType.EnableCameraForAllPlayers != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.EnableCameraForAllPlayers), bacType, bacType.EnableCameraForAllPlayers, value, "Camera EnableCameraForAllPlayers"));
                    bacType.EnableCameraForAllPlayers = value;
                    RaisePropertyChanged(() => EnableCameraForAllPlayers);
                    UpdateBacPlayer();
                }
            }
        }
        public bool ResetCameraAfterEnd
        {
            get
            {
                return bacType.I_74_1;
            }
            set
            {
                if (bacType.I_74_1 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.I_74_1), bacType, bacType.I_74_1, value, "Camera ResetCameraAfterEnd"));
                    bacType.I_74_1 = value;
                    RaisePropertyChanged(() => ResetCameraAfterEnd);
                    UpdateBacPlayer();
                }
            }
        }
        public bool FocusOnTarget
        {
            get
            {
                return bacType.FocusOnTarget;
            }
            set
            {
                if (bacType.FocusOnTarget != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.FocusOnTarget), bacType, bacType.FocusOnTarget, value, "Camera FocusOnTarget"));
                    bacType.FocusOnTarget = value;
                    RaisePropertyChanged(() => FocusOnTarget);
                    UpdateBacPlayer();
                }
            }
        }
        public bool DontFitToBodySize
        {
            get
            {
                return bacType.DontFitToBodySize;
            }
            set
            {
                if (bacType.DontFitToBodySize != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.DontFitToBodySize), bacType, bacType.DontFitToBodySize, value, "Camera DontFitToBodySize"));
                    bacType.DontFitToBodySize = value;
                    RaisePropertyChanged(() => DontFitToBodySize);
                    UpdateBacPlayer();
                }
            }
        }
        public bool Flag_74_4
        {
            get
            {
                return bacType.I_74_4;
            }
            set
            {
                if (bacType.I_74_4 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.I_74_4), bacType, bacType.I_74_4, value, "Camera Flag_74_4"));
                    bacType.I_74_4 = value;
                    RaisePropertyChanged(() => Flag_74_4);
                    UpdateBacPlayer();
                }
            }
        }
        public bool KeepWhileInputHeld
        {
            get
            {
                return bacType.I_74_5;
            }
            set
            {
                if (bacType.I_74_5 != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.I_74_5), bacType, bacType.I_74_5, value, "Camera KeepWhileInputHeld"));
                    bacType.I_74_5 = value;
                    RaisePropertyChanged(() => KeepWhileInputHeld);
                    UpdateBacPlayer();
                }
            }
        }
        public bool RequireTenHitCombo
        {
            get
            {
                return bacType.Secondary;
            }
            set
            {
                if (bacType.Secondary != value)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.Secondary), bacType, bacType.Secondary, value, "Camera RequireTenHitCombo"));
                    bacType.Secondary = value;
                    RaisePropertyChanged(() => RequireTenHitCombo);
                    UpdateBacPlayer();
                }
            }
        }

        public bool LockCameraPriority
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk9);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk9, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "Camera LockCameraPriority"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => LockCameraPriority);
                    UpdateBacPlayer();
                }
            }
        }
        public bool CameraFlags_Unk10
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk10);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk10, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk10"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk10);
                    UpdateBacPlayer();
                }
            }
        }
        public bool CameraFlags_Unk11
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk11);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk11, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk11"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_Unk11);
                    UpdateBacPlayer();
                }
            }
        }
        public bool CameraFlags_KeepAfterEnd
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.KeepAfterEnd);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.KeepAfterEnd, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags KeepAfterEnd"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_KeepAfterEnd);
                    UpdateBacPlayer();
                }
            }
        }
        public bool CameraFlags_UseType26Row
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.UseType26Row);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.UseType26Row, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags UseType26Row"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => CameraFlags_UseType26Row);
                    UpdateBacPlayer();
                }
            }
        }
        public bool DelayCameraBlend
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk14);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk14, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk14"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => DelayCameraBlend);
                    UpdateBacPlayer();
                }
            }
        }
        public bool ReverseCameraDirection
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk15);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk15, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Unk15"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => ReverseCameraDirection);
                    UpdateBacPlayer();
                }
            }
        }
        public bool ResumeFromEndView
        {
            get
            {
                return bacType.cameraFlags2.HasFlag(BAC_Type10.CameraFlags2.Unk16);
            }
            set
            {
                CameraFlags2 flags = bacType.cameraFlags2.SetFlag(BAC_Type10.CameraFlags2.Unk16, value);

                if (flags != bacType.cameraFlags2)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type10>(nameof(bacType.cameraFlags2), bacType, bacType.cameraFlags2, flags, "CameraFlags Resume From End View"));
                    bacType.cameraFlags2 = flags;
                    RaisePropertyChanged(() => ResumeFromEndView);
                    UpdateBacPlayer();
                }
            }
        }


    }
}
