using System;
using System.ComponentModel;
using System.Windows.Controls;
using XenoKit.Engine;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool RetainActionPosition
        {
            get => SceneManager.RetainActionMovement;
            set
            {
                SceneManager.RetainActionMovement = value;
                NotifyPropertyChanged(nameof(RetainActionPosition));
            }
        }
        public bool ScdPhysicsEnabled
        {
            get => Engine.Animation.ScdPhysics.Enabled;
            set
            {
                Engine.Animation.ScdPhysics.Enabled = value;
                NotifyPropertyChanged(nameof(ScdPhysicsEnabled));
            }
        }
        public bool ScdResetOnLoop
        {
            get => Engine.Animation.ScdPhysics.ResetOnLoop;
            set
            {
                Engine.Animation.ScdPhysics.ResetOnLoop = value;
                NotifyPropertyChanged(nameof(ScdResetOnLoop));
            }
        }
        public float ScdGravityScale
        {
            get => Engine.Animation.ScdPhysics.GravityScale;
            set
            {
                Engine.Animation.ScdPhysics.GravityScale = value;
                NotifyPropertyChanged(nameof(ScdGravityScale));
            }
        }
        public bool ScdShowColliders
        {
            get => Engine.Animation.ScdPhysics.ShowColliders;
            set
            {
                Engine.Animation.ScdPhysics.ShowColliders = value;
                NotifyPropertyChanged(nameof(ScdShowColliders));
            }
        }
        public bool SimulateBacMovement
        {
            get => Engine.Scripting.BAC.BacPlayer.SimulateMovement;
            set
            {
                Engine.Scripting.BAC.BacPlayer.SimulateMovement = value;
                NotifyPropertyChanged(nameof(SimulateBacMovement));
            }
        }
        public bool SimulateBacTracking
        {
            get => Engine.Scripting.BAC.BacPlayer.SimulateTracking;
            set
            {
                Engine.Scripting.BAC.BacPlayer.SimulateTracking = value;
                NotifyPropertyChanged(nameof(SimulateBacTracking));
            }
        }
        public bool SimulateBsaMovement
        {
            get => Engine.Scripting.BSA.ProjectileInstance.SimulateMovement;
            set
            {
                Engine.Scripting.BSA.ProjectileInstance.SimulateMovement = value;
                NotifyPropertyChanged(nameof(SimulateBsaMovement));
            }
        }
        public bool RenderBoneNames
        {
            get
            {
                return SettingsManager.settings.XenoKit_RenderBoneNames;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_RenderBoneNames != value)
                {
                    SettingsManager.settings.XenoKit_RenderBoneNames = value;
                    SettingsManager.Instance.SaveSettings();
                    NotifyPropertyChanged(nameof(RenderBoneNames));
                }
            }
        }
        public bool RenderBoneNamesMouseOver
        {
            get
            {
                return SettingsManager.settings.XenoKit_RenderBoneNamesMouseOverOnly;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_RenderBoneNamesMouseOverOnly != value)
                {
                    SettingsManager.settings.XenoKit_RenderBoneNamesMouseOverOnly = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool AutoResolvePasteReferences
        {
            get
            {
                return SettingsManager.settings.XenoKit_AutoResolvePasteReferences;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_AutoResolvePasteReferences != value)
                {
                    SettingsManager.settings.XenoKit_AutoResolvePasteReferences = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool FocusedBoneView
        {
            get
            {
                return SettingsManager.settings.XenoKit_HideLessImportantBones;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_HideLessImportantBones != value)
                {
                    SettingsManager.settings.XenoKit_HideLessImportantBones = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public float Scratches
        {
            get => SceneManager.BattleDamageScratches;
            set
            {
                SceneManager.BattleDamageScratches = value;
                NotifyPropertyChanged(nameof(Scratches));
            }
        }
        public float Blood
        {
            get => SceneManager.BattleDamageBlood;
            set
            {
                SceneManager.BattleDamageBlood = value;
                NotifyPropertyChanged(nameof(Blood));
            }
        }

        public SettingsPanel()
        {
            LoadSimulationSettings();
            InitializeComponent();
            DataContext = this;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
            SettingsManager.SettingsSaved += SaveSimulationSettings;
        }

        private static void LoadSimulationSettings()
        {
            Settings settings = SettingsManager.settings;
            SceneManager.RetainActionMovement = settings.XenoKit_RetainActionPosition;
            Engine.Animation.ScdPhysics.Enabled = settings.XenoKit_ScdPhysicsEnabled;
            Engine.Animation.ScdPhysics.ResetOnLoop = settings.XenoKit_ScdResetOnLoop;
            Engine.Animation.ScdPhysics.GravityScale = settings.XenoKit_ScdGravityScale;
            Engine.Animation.ScdPhysics.ShowColliders = settings.XenoKit_ScdShowColliders;
            Engine.Scripting.BAC.BacPlayer.SimulateMovement = settings.XenoKit_SimulateBacMovement;
            Engine.Scripting.BAC.BacPlayer.SimulateTracking = settings.XenoKit_SimulateBacTracking;
            Engine.Scripting.BSA.ProjectileInstance.SimulateMovement = settings.XenoKit_SimulateBsaMovement;
            SceneManager.BattleDamageScratches = settings.XenoKit_Scratches;
            SceneManager.BattleDamageBlood = settings.XenoKit_Blood;
        }

        private void SaveSimulationSettings(object sender, EventArgs args)
        {
            Settings settings = SettingsManager.settings;
            settings.XenoKit_RetainActionPosition = RetainActionPosition;
            settings.XenoKit_ScdPhysicsEnabled = ScdPhysicsEnabled;
            settings.XenoKit_ScdResetOnLoop = ScdResetOnLoop;
            settings.XenoKit_ScdGravityScale = ScdGravityScale;
            settings.XenoKit_ScdShowColliders = ScdShowColliders;
            settings.XenoKit_SimulateBacMovement = SimulateBacMovement;
            settings.XenoKit_SimulateBacTracking = SimulateBacTracking;
            settings.XenoKit_SimulateBsaMovement = SimulateBsaMovement;
            settings.XenoKit_Scratches = Scratches;
            settings.XenoKit_Blood = Blood;
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs args)
        {
            LoadSimulationSettings();
            NotifyPropertyChanged();
        }
    }
}
