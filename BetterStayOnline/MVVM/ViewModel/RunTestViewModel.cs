using BetterStayOnline.Core;
using BetterStayOnline.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline.MVVM.ViewModel
{
    class RunTestViewModel : ObservableObject
    {
        ResultList _results;

        public RelayCommand StartTestCommand { get; set; }

        private string _downloadSpeed;
        public string DownloadSpeed
        {
            get
            {
                return _downloadSpeed;
            }
            set
            {
                _downloadSpeed = value;
                OnPropertyChanged(nameof(DownloadSpeed));
            }
        }

        private string _uploadSpeed;
        public string UploadSpeed
        {
            get
            {
                return _uploadSpeed;
            }
            set
            {
                _uploadSpeed = value;
                OnPropertyChanged(nameof(UploadSpeed));
            }
        }

        public RunTestViewModel(ResultList results)
        {
            _results = results;

            StartTestCommand = new RelayCommand(o =>
            {
                int x = 0;

                DownloadSpeed = "40";
                UploadSpeed = "10";
            });
        }
    }
}
