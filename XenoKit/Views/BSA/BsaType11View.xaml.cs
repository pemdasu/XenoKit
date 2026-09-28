using System.Windows;
using System.Windows.Controls;
using XenoKit.ViewModel.BSA;

namespace XenoKit.Views.BSA
{
    public partial class BsaType11View : UserControl
    {
        public static readonly DependencyProperty BsaViewModelProperty = DependencyProperty.Register(
            nameof(BsaViewModel), typeof(BsaType11ViewModel), typeof(BsaType11View), new PropertyMetadata(null, BsaViewModelChanged));

        public BsaType11ViewModel BsaViewModel
        {
            get => (BsaType11ViewModel)GetValue(BsaViewModelProperty);
            set => SetValue(BsaViewModelProperty, value);
        }

        public BsaType11View()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

        private static void BsaViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((BsaType11View)sender).Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
