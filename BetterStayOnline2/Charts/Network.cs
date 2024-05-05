using BetterStayOnline2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline2.Charts
{
    public class Network : ObservableObject
    {
        public string name;
        public string isp;

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
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

        private bool _show;
        public bool Show
        {
            get { return _show; }
            set
            {
                _show = value;
                OnPropertyChanged();
            }
        }
    }
}
