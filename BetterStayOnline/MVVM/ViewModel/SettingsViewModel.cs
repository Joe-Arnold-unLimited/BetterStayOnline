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
        public RelayCommand SaveSettingsCommand { get; set; }

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




        public SettingsViewModel()
        {
            MinDownload = Configuration.MinDown();
            MinUpload = Configuration.MinUp();
            ShowMinDownload = Configuration.ShowMinDown();
            ShowMinUpload = Configuration.ShowMinUp();

            MinDownloadUpCommand = new RelayCommand(o => { MinDownload += 1; });
            MinDownloadDownCommand = new RelayCommand(o => { if (MinDownload > 0) MinDownload -= 1; });
            MinUploadUpCommand = new RelayCommand(o => { MinUpload += 1; });
            MinUploadDownCommand = new RelayCommand(o => { if (MinUpload > 0) MinUpload -= 1; });

            SaveSettingsCommand = new RelayCommand(o =>
            {
                Configuration.SetMinDown(MinDownload);
                Configuration.SetMinUp(MinUpload);
                Configuration.SetShowMinDown(ShowMinDownload);
                Configuration.SetShowMinUp(ShowMinUpload);
            });
        }
    }
}