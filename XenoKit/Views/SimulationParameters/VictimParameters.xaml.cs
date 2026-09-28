using LB_Common.Forms;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Engine;
using Xv2CoreLib.BDM;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Views.SimulationParameters
{
    /// <summary>
    /// Interaction logic for VictimParameters.xaml
    /// </summary>
    public partial class VictimParameters : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public bool VictimEnabled
        {
            get => SceneManager.VictimEnabled;
            set
            {
                if(value == true && SceneManager.Actors[1] == null)
                {
                    if (MessagePrompt.Show("Do you want to load the default victim character?\n\nIf you select no, you may manually load a character as usual, and then right-click on it in the outliner and set it as the Victim, but until you do so no victim will be available.", "No Victim Set", MessagePromptButtons.YesNo, MessagePromptIcon.Question) == MessagePromptResult.Yes)
                    {
                        SceneManager.EnsureActorIsSet(1);
                    }
                    else
                    {
                        return;
                    }
                }

                SceneManager.VictimEnabled = value;
                NotifyPropertyChanged(nameof(CanMoveVictim));
            }
        }

        public bool CanMoveVictim => SceneManager.VictimEnabled && SceneManager.Actors[1] != null &&
            SceneManager.IsOnTab(EditorTabs.Action, EditorTabs.Projectile);

        public bool VictimAutoRecover { get => SceneManager.VictimAutoRecover; set => SceneManager.VictimAutoRecover = value; }
        public double VictimRecoveryFrames { get => SceneManager.VictimRecoveryFrames; set => SceneManager.VictimRecoveryFrames = (int)value; }
        public bool VictimInvulnerable { get => SceneManager.VictimInvulnerable; set => SceneManager.VictimInvulnerable = value; }

        public double PositionX { get => SceneManager.VictimPosition.X; set => SceneManager.VictimPosition = new SimdVector3((float)value, SceneManager.VictimPosition.Y, SceneManager.VictimPosition.Z); }
        public double PositionY { get => SceneManager.VictimPosition.Y; set => SceneManager.VictimPosition = new SimdVector3(SceneManager.VictimPosition.X, (float)value, SceneManager.VictimPosition.Z); }
        public double PositionZ { get => SceneManager.VictimPosition.Z; set => SceneManager.VictimPosition = new SimdVector3(SceneManager.VictimPosition.X, SceneManager.VictimPosition.Y, (float)value); }
        public double RotationX { get => SceneManager.VictimRotation.X; set => SceneManager.VictimRotation = new SimdVector3((float)value, SceneManager.VictimRotation.Y, SceneManager.VictimRotation.Z); }
        public double RotationY { get => SceneManager.VictimRotation.Y; set => SceneManager.VictimRotation = new SimdVector3(SceneManager.VictimRotation.X, (float)value, SceneManager.VictimRotation.Z); }
        public double RotationZ { get => SceneManager.VictimRotation.Z; set => SceneManager.VictimRotation = new SimdVector3(SceneManager.VictimRotation.X, SceneManager.VictimRotation.Y, (float)value); }

        public VictimParameters()
        {
            DataContext = this;
            InitializeComponent();
            SceneManager.ActorChanged += SceneManager_ActorChanged;
            SceneManager.EditorTabChanged += SceneManager_EditorTabChanged;
            SceneManager.VictimTransformChanged += SceneManager_VictimTransformChanged;
            victimStateComboBox.SelectedIndex = 0;
        }

        private void SceneManager_EditorTabChanged(object sender, System.EventArgs e)
        {
            NotifyPropertyChanged(nameof(CanMoveVictim));
        }

        private void SceneManager_ActorChanged(object source, ActorChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(VictimEnabled));
            NotifyPropertyChanged(nameof(CanMoveVictim));
        }

        private void MoveVictim_Click(object sender, RoutedEventArgs e)
        {
            if (!CanMoveVictim || Viewport.Instance == null)
                return;

            Viewport.Instance.EntityTransformGizmo.SetContext(SceneManager.Actors[1], EditorTabs.Action);
            Viewport.Instance.EntityTransformGizmo.Enable();
        }

        private void SceneManager_VictimTransformChanged(object sender, System.EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new System.Action(() => SceneManager_VictimTransformChanged(sender, e)));
                return;
            }

            NotifyPropertyChanged(nameof(PositionX));
            NotifyPropertyChanged(nameof(PositionY));
            NotifyPropertyChanged(nameof(PositionZ));
            NotifyPropertyChanged(nameof(RotationX));
            NotifyPropertyChanged(nameof(RotationY));
            NotifyPropertyChanged(nameof(RotationZ));
        }

        private void victimStateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (victimStateComboBox.SelectedIndex)
            {
                case 1: SceneManager.VictimHitboxState = HitboxState.Guarding; break;
                case 2: SceneManager.VictimHitboxState = HitboxState.Stumble; break;
                case 3: SceneManager.VictimHitboxState = HitboxState.PrimaryKnockback; break;
                case 4: SceneManager.VictimHitboxState = HitboxState.FloatingKnockback; break;
                case 5: SceneManager.VictimHitboxState = HitboxState.GroundImpact; break;
                case 6: SceneManager.VictimHitboxState = HitboxState.KnockedDown; break;
                case 7: SceneManager.VictimHitboxState = HitboxState.Back; break;
                default: SceneManager.VictimHitboxState = null; break;
            }
        }
    }
}
