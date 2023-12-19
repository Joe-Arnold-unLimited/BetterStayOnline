using BetterStayOnline.Core;
using BetterStayOnline.MVVM.Model;
using BetterStayOnline.SpeedTest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace BetterStayOnline.MVVM.ViewModel
{
    class EventsViewModel : ObservableObject
    {
        public RelayCommand CreateEventCommand { get; set; }
        public RelayCommand RemoveEventCommand { get; set; }

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

        private string[] _minutes;
        public string[] Minutes
        {
            get { return _minutes; }
            set
            {
                _minutes = value;
                OnPropertyChanged();
            }
        }

        private string _selectedDay;
        public string SelectedDay
        {
            get { return _selectedDay; }
            set
            {
                _selectedDay = value;
                OnPropertyChanged();
            }
        }

        private string _selectedHour;
        public string SelectedHour
        {
            get { return _selectedHour; }
            set
            {
                _selectedHour = value;
                OnPropertyChanged();
            }
        }

        private string _selectedMinute;
        public string SelectedMinute
        {
            get { return _selectedMinute; }
            set
            {
                _selectedMinute = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Event> _eventList;
        public ObservableCollection<Event> EventList
        {
            get { return _eventList; }
            set
            {
                _eventList = value;
                OnPropertyChanged();
            }
        }



        public List<Timer> _eventRunners;
        private Func<bool> runSpeedTest = () => { Speedtester.RunSpeedTest(); return true; };

        public EventsViewModel()
        {
            Days = TimerFactory.Days;
            Hours = TimerFactory.Hours;
            Minutes = TimerFactory.Minutes;

            _selectedDay = Days[0];
            _selectedHour = Hours[0];
            _selectedMinute = Minutes[0];

            _eventList = EventReader.GetEvents();

            _eventRunners = new List<Timer>();
            _eventRunners = TimerFactory.CreateTimers(_eventRunners, _eventList, runSpeedTest).ToList();

            var threads = System.Diagnostics.Process.GetCurrentProcess().Threads;

            CreateEventCommand = new RelayCommand(o =>
            {
                if (_eventList.Count < 50)
                {
                    Event e = new Event()
                    {
                        Day = _selectedDay,
                        Hour = _selectedHour,
                        Minute = _selectedMinute,
                        Selected = false
                    };

                    EventReader.AddEvent(e);
                    _eventList.Add(e);
                    _eventRunners = TimerFactory.CreateTimers(_eventRunners, _eventList, runSpeedTest).ToList();
                }
            });

            RemoveEventCommand = new RelayCommand(o =>
            {
                foreach(var e in _eventList.Where(e => e.Selected).ToList())
                    _eventList .Remove(e);
                EventReader.SaveEvents(_eventList);
                _eventRunners = TimerFactory.CreateTimers(_eventRunners, _eventList, runSpeedTest).ToList();
            });
        }

        private void SetupEventRunners()
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            _eventRunners.Clear();

            foreach (var e in _eventList)
            {
                DateTime current = DateTime.Now;
                Timer timer;

                int daysUntil = 0;
                int daysBetweenRunning = 7;
                if(e.Day == "Every day")
                    daysBetweenRunning = 1;
                else
                {
                    DayOfWeek dayToRun = DayOfWeek.Monday;
                    switch (e.Day)
                    {
                        case "Monday": dayToRun = DayOfWeek.Monday; break;
                        case "Tuesday": dayToRun = DayOfWeek.Tuesday; break;
                        case "Wednesday": dayToRun = DayOfWeek.Wednesday; break;
                        case "Thursday": dayToRun = DayOfWeek.Thursday; break;
                        case "Friday": dayToRun = DayOfWeek.Friday; break;
                        case "Saturday": dayToRun = DayOfWeek.Saturday; break;
                        case "Sunday": dayToRun = DayOfWeek.Sunday; break;
                    }
                    daysUntil = ((int)dayToRun - (int)DateTime.Today.DayOfWeek + 7) % 7;
                }

                int hour = 0;
                if (e.Hour == "Midnight") hour = 0;
                else if (e.Hour == "Midday") hour = 12;
                else hour = int.Parse(e.Hour);

                int minute = int.Parse(e.Minute);

                int second = 0;
                if(hour == 0 && minute == 0)
                {
                    hour = 23;
                    minute = 59;
                    second = 59;
                }

                TimeSpan timeToGo = (new TimeSpan(hour, minute, second) - current.TimeOfDay) + TimeSpan.FromDays(daysUntil);
                if (timeToGo < TimeSpan.Zero)
                    timeToGo += TimeSpan.FromDays(daysBetweenRunning);

                if (timeToGo > TimeSpan.Zero)
                {
                    timer = new System.Threading.Timer(x =>
                    {
                        Speedtester.RunSpeedTest();
                    }, null, timeToGo, new TimeSpan(daysBetweenRunning, 0, 0, 0, 0));
                    _eventRunners.Add(timer);
                }
            }
        }
    }
}