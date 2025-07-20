using GalaSoft.MvvmLight.CommandWpf;
using LB_Common.Numbers;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EffectContainer;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for SceneCamera.xaml
    /// </summary>
    public partial class Screenshot : UserControl, INotifyPropertyChanged
    {
        #region NotPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private float _roll, _fieldOfView = EAN_File.DefaultFoV;

        public LocalSettings LocalSettings => LocalSettings.Instance;

        public CustomVector4 CameraPos { get; set; } = new CustomVector4(0, 1f, -5, 1);
        public CustomVector4 CameraTargetPos { get; set; } = new CustomVector4(0, 1f, 1f, 1);
        public float Roll
        {
            get => _roll;
            set
            {
                if (_roll != value)
                {
                    _roll = value;
                    NotifyPropertyChanged(nameof(Roll));
                }
            }
        }
        public float FieldOfView
        {
            get => _fieldOfView;
            set
            {
                if( _fieldOfView != value)
                {
                    _fieldOfView = value;
                    NotifyPropertyChanged(nameof(FieldOfView));
                }
            }
        }
        public System.Windows.Media.Color BackgroundColor
        {
            get => System.Windows.Media.Color.FromScRgb(Viewport.ScreenshotBackgroundColor.A / 255f, Viewport.ScreenshotBackgroundColor.R / 255f, Viewport.ScreenshotBackgroundColor.G / 255f, Viewport.ScreenshotBackgroundColor.B / 255f);
            set
            {
                Viewport.ScreenshotBackgroundColor = new Microsoft.Xna.Framework.Color(value.R, value.G, value.B, value.A);
            }
        }

        private bool cameraUpdateFromView = false;
        private int cameraUpdateFromValues = 0;

        public Screenshot()
        {
            InitializeComponent();
            CameraPos.PropertyChanged += CameraProperty_Changed;
            CameraTargetPos.PropertyChanged += CameraProperty_Changed;
            PropertyChanged += CameraProperty_Changed;

            Viewport.DelayedEventUpdateEvent += SceneManager_DelayedUpdate;
        }

        private void SceneManager_DelayedUpdate(object sender, EventArgs e)
        {
            if(cameraUpdateFromValues > 0)
            {
                cameraUpdateFromValues--;
                return;
            }

            cameraUpdateFromView = false;

            if(Viewport.Instance != null)
            {
                UpdateCameraValuesFromView();
            }
        }

        private void UpdateCameraValuesFromView()
        {
            if(Viewport.Instance.Camera.CameraState.Position.X != CameraPos.X ||
               Viewport.Instance.Camera.CameraState.Position.Y != CameraPos.Y ||
               Viewport.Instance.Camera.CameraState.Position.Z != CameraPos.Z ||
               Viewport.Instance.Camera.CameraState.TargetPosition.X != CameraTargetPos.X ||
               Viewport.Instance.Camera.CameraState.TargetPosition.Y != CameraTargetPos.Y ||
               Viewport.Instance.Camera.CameraState.TargetPosition.Z != CameraTargetPos.Z ||
               Viewport.Instance.Camera.CameraState.Roll != Roll ||
               Viewport.Instance.Camera.CameraState.FieldOfView != FieldOfView)
            {
                cameraUpdateFromView = true;

                CameraPos.X = Viewport.Instance.Camera.CameraState.Position.X;
                CameraPos.Y = Viewport.Instance.Camera.CameraState.Position.Y;
                CameraPos.Z = Viewport.Instance.Camera.CameraState.Position.Z;

                CameraTargetPos.X = Viewport.Instance.Camera.CameraState.TargetPosition.X;
                CameraTargetPos.Y = Viewport.Instance.Camera.CameraState.TargetPosition.Y;
                CameraTargetPos.Z = Viewport.Instance.Camera.CameraState.TargetPosition.Z;

                Roll = Viewport.Instance.Camera.CameraState.Roll;
                FieldOfView = Viewport.Instance.Camera.CameraState.FieldOfView;
            }
        }

        private void CameraProperty_Changed(object sender, PropertyChangedEventArgs e)
        {
            if(cameraUpdateFromView) return;

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            if(Viewport.Instance != null)
            {
                Viewport.Instance.Camera.CameraState.SetState(CameraPos, CameraTargetPos, _roll, _fieldOfView);
                cameraUpdateFromValues = 10;
                Log.Add("Updating camera state");
            }
        }


        public RelayCommand<int> ApplyCameraPresetCommand => new RelayCommand<int>(ApplyCameraPreset);
        private void ApplyCameraPreset(int slot)
        {
            if (slot < 0 || slot >= LocalSettings.Instance.CameraStates.Length) return;
            if (LocalSettings.Instance.CameraStates[slot] == null)
            {
                Log.Add("Cannot apply camera state as none exists in this slot.");
                return;
            }

            Viewport.Instance.Camera.CameraState.SetState(LocalSettings.Instance.CameraStates[slot]);
        }

        public RelayCommand<int> SaveCameraPresetCommand => new RelayCommand<int>(SaveCameraPreset);
        private void SaveCameraPreset(int slot)
        {
            if (slot < 0 || slot >= LocalSettings.Instance.CameraStates.Length) return;
            LocalSettings.Instance.CameraStates[slot] = new SerializedCameraState(Viewport.Instance.Camera.CameraState);
        }
    }
}
