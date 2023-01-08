using BetterStayOnline.Core;
using BetterStayOnline.SpeedTest;
using System.Threading;

namespace BetterStayOnline.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand RunTestViewCommand { get; set; }
        public RelayCommand ResultsViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }
        public RelayCommand TipsViewCommand { get; set; }

        public StartupProcedureViewModel StartProcedureVm { get; set; }
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

        private bool _homeChecked;
        public bool HomeChecked
        {
            get { return _homeChecked; }
            set
            {
                _homeChecked = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            StartProcedureVm = new StartupProcedureViewModel();
            HomeVm = new HomeViewModel();
            ResultsVm = new ResultsViewModel();
            SettingsVm = new SettingsViewModel();
            TipsVm = new TipsViewModel();
            CurrentView = StartProcedureVm;

            HomeViewCommand = new RelayCommand(o => { CurrentView = HomeVm; });
            ResultsViewCommand = new RelayCommand(o => { CurrentView = ResultsVm; });
            SettingsViewCommand = new RelayCommand(o => { CurrentView = SettingsVm; });
            TipsViewCommand = new RelayCommand(o => { CurrentView = TipsVm; });

            HomeViewCommand.ChangeCanExecute(false);
            ResultsViewCommand.ChangeCanExecute(false);
            SettingsViewCommand.ChangeCanExecute(false);
            TipsViewCommand.ChangeCanExecute(false);

            Thread thread = null;
            thread = new Thread(new ThreadStart(() =>
            {
                Speedtester.RunSpeedTest();

                HomeViewCommand.ChangeCanExecute(true);
                ResultsViewCommand.ChangeCanExecute(true);
                SettingsViewCommand.ChangeCanExecute(true);
                TipsViewCommand.ChangeCanExecute(true);

                HomeViewCommand.Execute(null);
                HomeChecked = true;
            }));
            thread.Start();
        }
    }
}
