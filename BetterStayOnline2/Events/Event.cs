using BetterStayOnline2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline2.Events
{
    public class Event : ObservableObject
    {
        public bool Equals(Event e)
        {
            if (e.Day == Day && e.Hour == Hour && e.Minute == Minute) return true;
            return false;
        }

        private string _day;
        public string Day
        {
            get { return _day; }
            set
            {
                _day = value;
                OnPropertyChanged();
            }
        }

        private string _hour;
        public string Hour
        {
            get { return _hour; }
            set
            {
                _hour = value;
                OnPropertyChanged();
            }
        }

        private string _minute;
        public string Minute
        {
            get { return _minute; }
            set
            {
                _minute = value;
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
