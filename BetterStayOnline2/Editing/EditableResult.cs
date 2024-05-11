using BetterStayOnline2.Core;
using System;

namespace BetterStayOnline2.Editing
{
    public class EditableResult : ObservableObject
    {
        private DateTime _datetime;
        public DateTime Datetime
        {
            get { return _datetime; }
            set
            {
                _datetime = value;
                OnPropertyChanged();
            }
        }

        public string DateConverted
        {
            get
            {
                return _datetime.ToString("dd MMM yyyy hh:mm");
            }
        }

        private double _downSpeed;
        public double DownSpeed
        {
            get { return _downSpeed; }
            set
            {
                _downSpeed = value;
                OnPropertyChanged();
            }
        }

        private double _upSpeed;
        public double UpSpeed
        {
            get { return _upSpeed; }
            set
            {
                _upSpeed = value;
                OnPropertyChanged();
            }
        }

        private string _isp;
        public string ISP
        {
            get { return _isp; }
            set
            {
                _isp = value;
                OnPropertyChanged();
            }
        }

        private string _networkName;
        public string NetworkName
        {
            get { return _networkName; }
            set
            {
                _networkName = value;
                OnPropertyChanged();
            }
        }

        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                OnPropertyChanged();
            }
        }
    }
}
