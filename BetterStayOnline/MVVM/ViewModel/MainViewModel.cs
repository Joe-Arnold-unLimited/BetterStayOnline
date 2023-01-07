using BetterStayOnline.Core;

namespace BetterStayOnline.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand RunTestViewCommand { get; set; }
        public RelayCommand ResultsViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }
        public RelayCommand TipsViewCommand { get; set; }

        public HomeViewModel HomeVm { get; set; }
        public ResultsViewModel ResultsVm { get; set; }
        public SettingsViewModel SettingsVm { get; set; }
        public TipsViewModel TipsVm { get; set; }

        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set 
            { 
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            HomeVm = new HomeViewModel();
            ResultsVm = new ResultsViewModel();
            SettingsVm = new SettingsViewModel();
            TipsVm = new TipsViewModel();
            CurrentView = HomeVm;

            HomeViewCommand = new RelayCommand(o => { CurrentView = HomeVm; });
            ResultsViewCommand = new RelayCommand(o => { CurrentView = ResultsVm; });
            SettingsViewCommand = new RelayCommand(o => { CurrentView = SettingsVm; });
            TipsViewCommand = new RelayCommand(o => { CurrentView = TipsVm; });
        }
    }
}
