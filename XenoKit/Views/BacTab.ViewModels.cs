using System;
using System.ComponentModel;
using XenoKit.ViewModel.BAC;
using Xv2CoreLib.BAC;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for BacTab.xaml
    /// </summary>
    public partial class BacTab
    {
        #region ViewModels
        //Current view models are kept around so they can the event references can be cleared up properly
        private IDisposable _lastViewModelBase = null;
        private IDisposable _lastViewModel = null;

        private object SetBaseViewModel(IDisposable viewModel)
        {
            if(viewModel != null)
            {
                if (_lastViewModelBase != null)
                    _lastViewModelBase.Dispose();

                _lastViewModelBase = viewModel;
            }

            return viewModel;
        }

        private object SetViewModel(IDisposable viewModel)
        {
            if (viewModel != null)
            {
                if (_lastViewModel is INotifyPropertyChanged previous)
                    previous.PropertyChanged -= BacViewModel_PropertyChanged;
                if (_lastViewModel != null)
                    _lastViewModel.Dispose();

                _lastViewModel = viewModel;
                if (viewModel is INotifyPropertyChanged current)
                    current.PropertyChanged += BacViewModel_PropertyChanged;
            }

            return viewModel;
        }

        private void BacViewModel_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(BAC_TypeBase.Type) && args.PropertyName != nameof(BAC_TypeBase.DisplayName))
                SelectedBacType?.RefreshType();
        }

        public BACTypeBaseViewModel BacTypeBaseViewModel => (BACTypeBaseViewModel)SetBaseViewModel((SelectedBacType is BAC_TypeBase) ? new BACTypeBaseViewModel(SelectedBacType as BAC_TypeBase) : null);
        public BACType0ViewModel BacType0ViewModel => (BACType0ViewModel)SetViewModel((SelectedBacType is BAC_Type0) ? new BACType0ViewModel(SelectedBacType as BAC_Type0) : null);
        public BACType1ViewModel BacType1ViewModel => (BACType1ViewModel)SetViewModel((SelectedBacType is BAC_Type1) ? new BACType1ViewModel(SelectedBacType as BAC_Type1) : null);
        public BACType2ViewModel BacType2ViewModel => (BACType2ViewModel)SetViewModel((SelectedBacType is BAC_Type2) ? new BACType2ViewModel(SelectedBacType as BAC_Type2) : null);
        public BACType3ViewModel BacType3ViewModel => (BACType3ViewModel)SetViewModel((SelectedBacType is BAC_Type3) ? new BACType3ViewModel(SelectedBacType as BAC_Type3) : null);
        public BACType4ViewModel BacType4ViewModel => (BACType4ViewModel)SetViewModel((SelectedBacType is BAC_Type4) ? new BACType4ViewModel(SelectedBacType as BAC_Type4) : null);
        public BACType5ViewModel BacType5ViewModel => (BACType5ViewModel)SetViewModel((SelectedBacType is BAC_Type5) ? new BACType5ViewModel(SelectedBacType as BAC_Type5) : null);
        public BACType6ViewModel BacType6ViewModel => (BACType6ViewModel)SetViewModel((SelectedBacType is BAC_Type6) ? new BACType6ViewModel(SelectedBacType as BAC_Type6) : null);
        public BACType7ViewModel BacType7ViewModel => (BACType7ViewModel)SetViewModel((SelectedBacType is BAC_Type7) ? new BACType7ViewModel(SelectedBacType as BAC_Type7) : null);
        public BACType8ViewModel BacType8ViewModel => (BACType8ViewModel)SetViewModel((SelectedBacType is BAC_Type8) ? new BACType8ViewModel(SelectedBacType as BAC_Type8) : null);
        public BACType9ViewModel BacType9ViewModel => (BACType9ViewModel)SetViewModel((SelectedBacType is BAC_Type9) ? new BACType9ViewModel(SelectedBacType as BAC_Type9) : null);
        public BACType10ViewModel BacType10ViewModel => (BACType10ViewModel)SetViewModel((SelectedBacType is BAC_Type10) ? new BACType10ViewModel(SelectedBacType as BAC_Type10) : null);
        public BACType11ViewModel BacType11ViewModel => (BACType11ViewModel)SetViewModel((SelectedBacType is BAC_Type11) ? new BACType11ViewModel(SelectedBacType as BAC_Type11) : null);
        public BACType12ViewModel BacType12ViewModel => (BACType12ViewModel)SetViewModel((SelectedBacType is BAC_Type12) ? new BACType12ViewModel(SelectedBacType as BAC_Type12) : null);
        public BACType13ViewModel BacType13ViewModel => (BACType13ViewModel)SetViewModel((SelectedBacType is BAC_Type13) ? new BACType13ViewModel(SelectedBacType as BAC_Type13) : null);
        public BACType14ViewModel BacType14ViewModel => (BACType14ViewModel)SetViewModel((SelectedBacType is BAC_Type14) ? new BACType14ViewModel(SelectedBacType as BAC_Type14) : null);
        public BACType15ViewModel BacType15ViewModel => (BACType15ViewModel)SetViewModel((SelectedBacType is BAC_Type15) ? new BACType15ViewModel(SelectedBacType as BAC_Type15) : null);
        public BACType16ViewModel BacType16ViewModel => (BACType16ViewModel)SetViewModel((SelectedBacType is BAC_Type16) ? new BACType16ViewModel(SelectedBacType as BAC_Type16) : null);
        public BACType17ViewModel BacType17ViewModel => (BACType17ViewModel)SetViewModel((SelectedBacType is BAC_Type17) ? new BACType17ViewModel(SelectedBacType as BAC_Type17) : null);
        public BACType18ViewModel BacType18ViewModel => (BACType18ViewModel)SetViewModel((SelectedBacType is BAC_Type18) ? new BACType18ViewModel(SelectedBacType as BAC_Type18) : null);
        public BACType19ViewModel BacType19ViewModel => (BACType19ViewModel)SetViewModel((SelectedBacType is BAC_Type19) ? new BACType19ViewModel(SelectedBacType as BAC_Type19) : null);
        public BACType20ViewModel BacType20ViewModel => (BACType20ViewModel)SetViewModel((SelectedBacType is BAC_Type20) ? new BACType20ViewModel(SelectedBacType as BAC_Type20) : null);
        public BACType21ViewModel BacType21ViewModel => (BACType21ViewModel)SetViewModel((SelectedBacType is BAC_Type21) ? new BACType21ViewModel(SelectedBacType as BAC_Type21) : null);
        public BACType22ViewModel BacType22ViewModel => (BACType22ViewModel)SetViewModel((SelectedBacType is BAC_Type22) ? new BACType22ViewModel(SelectedBacType as BAC_Type22) : null);
        public BACType23ViewModel BacType23ViewModel => (BACType23ViewModel)SetViewModel((SelectedBacType is BAC_Type23) ? new BACType23ViewModel(SelectedBacType as BAC_Type23) : null);
        public BACType24ViewModel BacType24ViewModel => (BACType24ViewModel)SetViewModel((SelectedBacType is BAC_Type24) ? new BACType24ViewModel(SelectedBacType as BAC_Type24) : null);
        public BACType25ViewModel BacType25ViewModel => (BACType25ViewModel)SetViewModel((SelectedBacType is BAC_Type25) ? new BACType25ViewModel(SelectedBacType as BAC_Type25) : null);
        public BACType26ViewModel BacType26ViewModel => (BACType26ViewModel)SetViewModel((SelectedBacType is BAC_Type26) ? new BACType26ViewModel(SelectedBacType as BAC_Type26) : null);
        public BACType27ViewModel BacType27ViewModel => (BACType27ViewModel)SetViewModel((SelectedBacType is BAC_Type27) ? new BACType27ViewModel(SelectedBacType as BAC_Type27) : null);
        public BACType28ViewModel BacType28ViewModel => (BACType28ViewModel)SetViewModel((SelectedBacType is BAC_Type28) ? new BACType28ViewModel(SelectedBacType as BAC_Type28) : null);
        public BACType29ViewModel BacType29ViewModel => (BACType29ViewModel)SetViewModel((SelectedBacType is BAC_Type29) ? new BACType29ViewModel(SelectedBacType as BAC_Type29) : null);
        public BACType30ViewModel BacType30ViewModel => (BACType30ViewModel)SetViewModel((SelectedBacType is BAC_Type30) ? new BACType30ViewModel(SelectedBacType as BAC_Type30) : null);
        public BACType31ViewModel BacType31ViewModel => (BACType31ViewModel)SetViewModel((SelectedBacType is BAC_Type31) ? new BACType31ViewModel(SelectedBacType as BAC_Type31) : null);

        #endregion
    }
}
