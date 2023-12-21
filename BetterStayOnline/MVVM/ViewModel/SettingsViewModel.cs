using BetterStayOnline.Core;
using BetterStayOnline.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline.MVVM.ViewModel
{
    class SettingsViewModel : ObservableObject
    {
        public RelayCommand MinDownloadUpCommand { get; set; }
        public RelayCommand MinDownloadDownCommand { get; set; }
        public RelayCommand MinUploadUpCommand { get; set; }
        public RelayCommand MinUploadDownCommand { get; set; }
        public RelayCommand DownloadRangeUpCommand { get; set; }
        public RelayCommand DownloadRangeDownCommand { get; set; }
        public RelayCommand UploadRangeUpCommand { get; set; }
        public RelayCommand UploadRangeDownCommand { get; set; }
        public RelayCommand DaysForAverageUpCommand { get; set; }
        public RelayCommand DaysForAverageDownCommand { get; set; }
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

        private int _downloadRange;
        public int DownloadRange
        {
            get { return _downloadRange; }
            set
            {
                _downloadRange = value;
                OnPropertyChanged();
            }
        }

        private int _uploadRange;
        public int UploadRange
        {
            get { return _uploadRange; }
            set
            {
                _uploadRange = value;
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

        private bool _showPercentagesBelowMinimums;
        public bool ShowPercentagesBelowMinimums
        {
            get { return _showPercentagesBelowMinimums; }
            set
            {
                _showPercentagesBelowMinimums = value;
                OnPropertyChanged();
            }
        }

        private bool _showDownloadRange;
        public bool ShowDownloadRange
        {
            get { return _showDownloadRange; }
            set
            {
                _showDownloadRange = value;
                OnPropertyChanged();
            }
        }

        private bool _showUploadRange;
        public bool ShowUploadRange
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

        public SettingsViewModel()
        {
            ShowDownloadPoints = Configuration.ShowDownloadPoints();
            ShowUploadPoints = Configuration.ShowUploadPoints();
            ShowDownloadTrendline = Configuration.ShowDownloadTrendline();
            ShowUploadTrendline = Configuration.ShowUploadTrendline();
            DaysForAverage = Configuration.DaysForAverage();
            MinDownload = Configuration.MinDown();
            MinUpload = Configuration.MinUp();
            DownloadRange = (int)(Configuration.DownloadRange() * 100);
            UploadRange = (int)(Configuration.UploadRange() * 100);
            ShowMinDownload = Configuration.ShowMinDown();
            ShowMinUpload = Configuration.ShowMinUp();
            ShowDownloadRange = Configuration.ShowDownloadRange();
            ShowUploadRange = Configuration.ShowUploadRange();
            ShowPercentagesBelowMinimums = Configuration.ShowPercentagesBelowMinimums();
            RunSpeedtestOnStartup = Configuration.RunTestOnStartup();

            DaysForAverageUpCommand = new RelayCommand(o => { if (DaysForAverage < 14) DaysForAverage++; });
            DaysForAverageDownCommand = new RelayCommand(o => { if (DaysForAverage > 0) DaysForAverage--; });
            MinDownloadUpCommand = new RelayCommand(o => { MinDownload++; });
            MinDownloadDownCommand = new RelayCommand(o => { if (MinDownload > 0) MinDownload--; });
            MinUploadUpCommand = new RelayCommand(o => { MinUpload++; });
            MinUploadDownCommand = new RelayCommand(o => { if (MinUpload > 0) MinUpload--; });
            DownloadRangeUpCommand = new RelayCommand(o => { if(DownloadRange <= 95) DownloadRange+=5; });
            DownloadRangeDownCommand = new RelayCommand(o => { if (UploadRange > 0) DownloadRange-=5; });
            UploadRangeUpCommand = new RelayCommand(o => { if (UploadRange <= 95) UploadRange += 5; });
            UploadRangeDownCommand = new RelayCommand(o => { if (UploadRange > 0) UploadRange-=5; });

            SaveSettingsCommand = new RelayCommand(o =>
            {
                Configuration.SetShowDownloadPoints(ShowDownloadPoints);
                Configuration.SetShowUploadPoints(ShowUploadPoints);
                Configuration.SetShowDownloadTrendline(ShowDownloadTrendline);
                Configuration.SetShowUploadTrendline(ShowUploadTrendline);
                Configuration.SetDaysForAverage(DaysForAverage);
                Configuration.SetMinDown(MinDownload);
                Configuration.SetMinUp(MinUpload);
                Configuration.SetDownloadRange(DownloadRange);
                Configuration.SetUploadRange(UploadRange);
                Configuration.SetShowMinDown(ShowMinDownload);
                Configuration.SetShowMinUp(ShowMinUpload);
                Configuration.SetShowDownloadRange(ShowDownloadRange);
                Configuration.SetShowUploadRange(ShowUploadRange);
                Configuration.SetShowPercentagesBelowMinimums(ShowPercentagesBelowMinimums);
            });
        }
    }
}