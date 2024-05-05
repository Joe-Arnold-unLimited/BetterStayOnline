using BetterStayOnline2.Charts;
using BetterStayOnline2.Core;
using BetterStayOnline2.Events;
using System.Collections.ObjectModel;

namespace BetterStayOnline2.MVVM.ViewModel
{
    class SettingsViewModel : ObservableObject
    {
        public RelayCommand DaysForAverageUpCommand { get; set; }
        public RelayCommand DaysForAverageDownCommand { get; set; }
        public RelayCommand MinDownloadUpCommand { get; set; }
        public RelayCommand MinDownloadDownCommand { get; set; }
        public RelayCommand MinUploadUpCommand { get; set; }
        public RelayCommand MinUploadDownCommand { get; set; }
        public RelayCommand SaveSettingsCommand { get; set; }


        private bool _showDownloadPoints;
        public bool ShowDownloadPoints
        {
            get { return _showDownloadPoints; }
            set
            {
                _showDownloadPoints = value;
                OnPropertyChanged();
            }
        }

        private bool _showUploadPoints;
        public bool ShowUploadPoints
        {
            get { return _showUploadPoints; }
            set
            {
                _showUploadPoints = value;
                OnPropertyChanged();
            }
        }

        private bool _showOutages;
        public bool ShowOutages
        {
            get { return _showOutages; }
            set
            {
                _showOutages = value;
                OnPropertyChanged();
            }
        }

        private int _minDownload;
        public int MinDownload
        {
            get { return _minDownload; }
            set
            {
                _minDownload = value;
                OnPropertyChanged();
            }
        }

        private int _minUpload;
        public int MinUpload
        {
            get { return _minUpload; }
            set
            {
                _minUpload = value;
                OnPropertyChanged();
            }
        }

        public string[] CandlePeriods
        {
            get
            {
                return new string[] { "Monthly", "Weekly", "Daily" };
            }
        }

        private string _candlePeriod;
        public string CandlePeriod
        {
            get { return _candlePeriod; }
            set
            {
                _candlePeriod = value;
                OnPropertyChanged();
            }
        }

        private int _daysForAverage;
        public int DaysForAverage
        {
            get { return _daysForAverage; }
            set
            {
                _daysForAverage = value;
                OnPropertyChanged();
            }
        }

        private bool _showMinDownload;
        public bool ShowMinDownload
        {
            get { return _showMinDownload; }
            set
            {
                _showMinDownload = value;
                OnPropertyChanged();
            }
        }

        private bool _showMinUpload;
        public bool ShowMinUpload
        {
            get { return _showMinUpload; }
            set
            {
                _showMinUpload = value;
                OnPropertyChanged();
            }
        }

        private bool _ShowPercentagesAboveMinimums;
        public bool ShowPercentagesAboveMinimums
        {
            get { return _ShowPercentagesAboveMinimums; }
            set
            {
                _ShowPercentagesAboveMinimums = value;
                OnPropertyChanged();
            }
        }

        private bool _showDownloadRange;
        public bool ShowDownloadCandles
        {
            get { return _showDownloadRange; }
            set
            {
                _showDownloadRange = value;
                OnPropertyChanged();
            }
        }

        private bool _showUploadRange;
        public bool ShowUploadCandles
        {
            get { return _showUploadRange; }
            set
            {
                _showUploadRange = value;
                OnPropertyChanged();
            }
        }

        private bool _showDownloadTrendline;
        public bool ShowDownloadTrendline
        {
            get { return _showDownloadTrendline; }
            set
            {
                _showDownloadTrendline = value;
                OnPropertyChanged();
            }
        }

        private bool _showUploadTrendline;
        public bool ShowUploadTrendline
        {
            get { return _showUploadTrendline; }
            set
            {
                _showUploadTrendline = value;
                OnPropertyChanged();
            }
        }

        private bool _runSpeedtestOnStartup;
        public bool RunSpeedtestOnStartup
        {
            get { return _runSpeedtestOnStartup; }
            set
            {
                _runSpeedtestOnStartup = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Network> _networkList;
        public ObservableCollection<Network> NetworkList
        {
            get { return _networkList; }
            set
            {
                _networkList = value;
                OnPropertyChanged();
            }
        }

        public SettingsViewModel()
        {
            NetworkList = new ObservableCollection<Network>();

            ShowDownloadPoints = Configuration.ShowDownloadPoints();
            ShowUploadPoints = Configuration.ShowUploadPoints();
            ShowOutages = Configuration.ShowOutages();
            ShowDownloadTrendline = Configuration.ShowDownloadTrendline();
            ShowUploadTrendline = Configuration.ShowUploadTrendline();
            DaysForAverage = Configuration.DaysForAverage();
            MinDownload = Configuration.MinDown();
            MinUpload = Configuration.MinUp();
            ShowMinUpload = Configuration.ShowMinUp();
            ShowDownloadCandles = Configuration.ShowDownloadCandles();
            ShowUploadCandles = Configuration.ShowUploadCandles();
            CandlePeriod = Configuration.CandlePeriod();
            ShowPercentagesAboveMinimums = Configuration.ShowPercentagesAboveMinimums();
            RunSpeedtestOnStartup = Configuration.RunTestOnStartup();

            DaysForAverageUpCommand = new RelayCommand(o => { if (DaysForAverage < 14) DaysForAverage++; });
            DaysForAverageDownCommand = new RelayCommand(o => { if (DaysForAverage > 1) DaysForAverage--; });
            MinDownloadUpCommand = new RelayCommand(o => { MinDownload++; });
            MinDownloadDownCommand = new RelayCommand(o => { if (MinDownload > 1) MinDownload--; });
            MinUploadUpCommand = new RelayCommand(o => { MinUpload++; });
            MinUploadDownCommand = new RelayCommand(o => { if (MinUpload > 1) MinUpload--; });

            SaveSettingsCommand = new RelayCommand(o =>
            {
                Configuration.SetShowDownloadPoints(ShowDownloadPoints);
                Configuration.SetShowUploadPoints(ShowUploadPoints);
                Configuration.SetShowOutages(ShowOutages);
                Configuration.SetShowDownloadTrendline(ShowDownloadTrendline);
                Configuration.SetShowUploadTrendline(ShowUploadTrendline);
                Configuration.SetDaysForAverage(DaysForAverage);
                Configuration.SetMinDown(MinDownload);
                Configuration.SetMinUp(MinUpload);
                Configuration.SetShowMinDown(ShowMinDownload);
                Configuration.SetShowMinUp(ShowMinUpload);
                Configuration.SetShowDownloadCandles(ShowDownloadCandles);
                Configuration.SetShowUploadCandles(ShowUploadCandles);
                Configuration.SetCandlePeriod(CandlePeriod);
                Configuration.SetShowPercentagesAboveMinimums(ShowPercentagesAboveMinimums);
                Configuration.SetRunTestOnStartup(RunSpeedtestOnStartup);
            });
        }
    }
}