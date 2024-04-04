using BetterStayOnline2.Charts;
using BetterStayOnline2.ConnectionLoss;
using BetterStayOnline2.Core;
using LiveChartsCore.Defaults;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BetterStayOnline2.MVVM.ViewModel
{
    public static class CurrentContext {
        public static string currentView = "";
    }

    internal class MainViewModel : ObservableObject
    {
        public RelayCommand homeViewCommand { get; set; }
        public RelayCommand settingsViewCommand { get; set; }
        public RelayCommand eventsViewCommand { get; set; }

        public HomeViewModel homeVM { get; set; }
        public SettingsViewModel settingsVM { get; set; }
        public EventsViewModel eventsVM { get; set; }


        public DownDetector downDetector;

        private bool _testRunning;
        public bool testRunning
        {
            get
            {
                return _testRunning;
            }
            set
            {
                _testRunning = value;
                OnPropertyChanged();
            }
        }
        public ICommand RunTaskCommand { get; }

        private Color _appColor = Color.FromArgb(0, PlotManager.appColor.R, PlotManager.appColor.G, PlotManager.appColor.B);
        public string appColor
        {
            get
            {
                return "#" + _appColor.R.ToString("X2") + _appColor.G.ToString("X2") + _appColor.B.ToString("X2");
            }
            set
            {
                _appColor = ColorTranslator.FromHtml(value);
                OnPropertyChanged();
            }
        }

        private object _currentView;
        public object currentView 
        { 
            get {  return _currentView; }
            set
            {
                _currentView = value;
                CurrentContext.currentView = _currentView.GetType().Name;
                OnPropertyChanged();
            }
        }



        public MainViewModel()
        {
            homeVM = new HomeViewModel();
            settingsVM = new SettingsViewModel();
            eventsVM = new EventsViewModel();

            currentView = homeVM;

            homeViewCommand = new RelayCommand(o =>
            {
                currentView = homeVM;
            });

            settingsViewCommand = new RelayCommand(o =>
            {
                currentView = settingsVM;
            });

            eventsViewCommand = new RelayCommand(o =>
            {
                currentView = eventsVM;
            });

            downDetector = new DownDetector();

            RunTaskCommand = new RelayCommand(ExecuteRunTask, CanExecuteRunTask);
            PlotManager.TestStarted += PlotManager_TestStarted;
            PlotManager.TestCompleted += PlotManager_TestCompleted;
        }

        private void PlotManager_TestStarted(object sender, EventArgs e)
        {
            testRunning = true;
        }

        public event EventHandler TaskCompleted;

        private void PlotManager_TestCompleted(object sender, EventArgs e)
        {
            testRunning = false;
        }

        private bool CanExecuteRunTask(object parameter)
        {
            return !testRunning;
        }

        private void ExecuteRunTask(object parameter)
        {
            PlotManager.RunSpeedtest();
        }
    }
}
