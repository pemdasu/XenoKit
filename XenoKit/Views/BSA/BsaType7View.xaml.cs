using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.ViewModel.BSA;
using Xv2CoreLib.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaType7View : UserControl
    {
        public static readonly DependencyProperty BsaViewModelProperty = DependencyProperty.Register(
            nameof(BsaViewModel), typeof(BsaType7ViewModel), typeof(BsaType7View), new PropertyMetadata(null, BsaViewModelChanged));

        public BsaType7ViewModel BsaViewModel
        {
            get => (BsaType7ViewModel)GetValue(BsaViewModelProperty);
            set => SetValue(BsaViewModelProperty, value);
        }

        public BsaType7View()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

        private static void BsaViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((BsaType7View)sender).Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SoundPreview_Click(object sender, RoutedEventArgs e)
        {
            if (BsaViewModel == null)
                return;

            PreviewSound(BsaViewModel.AcbType, BsaViewModel.CueId);
        }

        internal static void PreviewSound(AcbType soundType, ushort cueId)
        {
            if (!BsaSoundResources.TryGetBacAcbType(soundType, out Xv2CoreLib.BAC.AcbType acbType))
            {
                Log.Add($"No XenoKit ACB mapping for BSA sound category {(ushort)soundType}.");
                return;
            }

            var acb = Files.Instance.GetAcbFile(acbType, Files.Instance.SelectedMove, SceneManager.Actors[0], true);

            if (acb == null)
            {
                Log.Add($"Could not find the ACB for AcbType {soundType}. Preview failed.");
                return;
            }

            if (cueId != ushort.MaxValue)
                Viewport.Instance.AudioEngine.PreviewCue(cueId, acb);
        }
    }
}
