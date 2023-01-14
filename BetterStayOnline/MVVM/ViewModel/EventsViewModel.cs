using BetterStayOnline.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline.MVVM.ViewModel
{
    class EventsViewModel : ObservableObject
    {
        private string[] _days;
        public string[] Days
        {
            get { return _days; }
            set
            {
                _days = value;
                OnPropertyChanged();
            }
        }

        private string[] _hours;
        public string[] Hours
        {
            get { return _hours; }
            set
            {
                _hours = value;
                OnPropertyChanged();
            }
        }

        public EventsViewModel()
        {
            Days = new string[] { "Every day", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            Hours = new string[] { "Midnight", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "Midday", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
        }
    }
}