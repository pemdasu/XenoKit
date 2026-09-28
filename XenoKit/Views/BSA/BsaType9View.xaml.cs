using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaType9View : UserControl
    {
        public static readonly DependencyProperty BsaViewModelProperty = DependencyProperty.Register(
            nameof(BsaViewModel), typeof(BsaType9ViewModel), typeof(BsaType9View), new PropertyMetadata(null, BsaViewModelChanged));

        public BsaType9ViewModel BsaViewModel
        {
            get => (BsaType9ViewModel)GetValue(BsaViewModelProperty);
            set => SetValue(BsaViewModelProperty, value);
        }

        public BsaType9View()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

        private static void BsaViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((BsaType9View)sender).Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
