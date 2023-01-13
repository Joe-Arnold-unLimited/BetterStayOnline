using BetterStayOnline.Core;
using BetterStayOnline.MVVM.Model;
using BetterStayOnline.SpeedTest;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;

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

        private bool _homeButtonIsEnabled;
        public bool HomeButtonIsEnabled
        {
            get { return _homeButtonIsEnabled; }
            set
            {
                _homeButtonIsEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool _resultsButtonIsEnabled;
        public bool ResultsButtonIsEnabled
        {
            get { return _resultsButtonIsEnabled; }
            set
            {
                _resultsButtonIsEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool _settingsButtonIsEnabled;
        public bool SettingsButtonIsEnabled
        {
            get { return _settingsButtonIsEnabled; }
            set
            {
                _settingsButtonIsEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool _tipsButtonIsEnabled;
        public bool TipsButtonIsEnabled
        {
            get { return _tipsButtonIsEnabled; }
            set
            {
                _tipsButtonIsEnabled = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            HomeButtonIsEnabled = false;
            ResultsButtonIsEnabled = false;
            SettingsButtonIsEnabled = false;
            TipsButtonIsEnabled = false;

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

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
            {
#if DEBUG == False
                if(Configuration.RunTestOnStartup()){
                    try { Speedtester.RunSpeedTest(); }
                    catch (Exception) { }
                }
#endif
            };

            //This event is raise on DoWork complete
            worker.RunWorkerCompleted += (o, ea) =>
            {
                CurrentView = HomeVm;
                HomeChecked = true;
                HomeButtonIsEnabled = true;
                ResultsButtonIsEnabled = true;
                SettingsButtonIsEnabled = true;
                TipsButtonIsEnabled = true;
            };

            worker.RunWorkerAsync();
        }
    }
}
