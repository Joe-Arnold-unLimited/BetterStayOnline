using BetterStayOnline2.Charts;
using BetterStayOnline2.Core;
using BetterStayOnline2.Events;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace BetterStayOnline2.MVVM.ViewModel
{
    internal class EventsViewModel : ObservableObject
    {
        public RelayCommand CreateEventCommand { get; set; }
        public RelayCommand RemoveEventCommand { get; set; }
        public RelayCommand RemoveAllEventsCommand { get; set; }

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

        public static ICollection<Timer> _eventRunners;
        public Func<bool> runSpeedTest = () => { PlotManager.RunSpeedtest(); return true; };

        public EventsViewModel()
        {
            Days = TimerFactory.Days;
            Hours = new string[] { "Every hour", "Every 2 hours", "Every 4 hours" }.Concat(TimerFactory.Hours).ToArray();
            Minutes = TimerFactory.Minutes;

            _selectedDay = Days[0];
            _selectedHour = Hours[0];
            _selectedMinute = Minutes[0];

            _eventList = EventCoordinator.GetEvents();

            _eventList = SortEvents(_eventList);

            _eventRunners = new List<Timer>();
            RefreshEventRunners();

            var threads = System.Diagnostics.Process.GetCurrentProcess().Threads;

            CreateEventCommand = new RelayCommand(o =>
            {
                int maxNumberOfEvents = 9999;

                if (_eventList.Count < maxNumberOfEvents)
                {
                    Event e;
                    switch (_selectedHour)
                    {
                        case "Every hour":
                        case "Every 2 hours":
                        case "Every 4 hours":
                            int numberOfHours = _selectedHour.Contains("2") ? 2 : _selectedHour.Contains("4") ? 4 : 1;

                            for (int hour = 0; hour < 24 && _eventList.Count < maxNumberOfEvents; hour += numberOfHours)
                            {
                                e = new Event()
                                {
                                    Day = _selectedDay,
                                    Hour = TimerFactory.Hours[hour],
                                    Minute = _selectedMinute,
                                    Selected = false
                                };

                                if (_eventList.All(item => !item.Equals(e)))
                                {
                                    EventCoordinator.AddEvent(e);
                                    _eventList.Add(e);
                                }
                            }
                            break;
                        default:
                            e = new Event()
                            {
                                Day = _selectedDay,
                                Hour = _selectedHour,
                                Minute = _selectedMinute,
                                Selected = false
                            };

                            if (_eventList.All(item => !item.Equals(e)))
                            {
                                EventCoordinator.AddEvent(e);
                                _eventList.Add(e);
                            }
                            break;
                    }

                    _eventList = SortEvents(_eventList);
                    RefreshEventRunners();
                }
            });

            RemoveEventCommand = new RelayCommand(o =>
            {
                foreach (var e in _eventList.Where(e => e.Selected).ToList())
                    _eventList.Remove(e);
                EventCoordinator.SaveEvents(_eventList);

                _eventList = SortEvents(_eventList);
                RefreshEventRunners();
            });

            RemoveAllEventsCommand = new RelayCommand(o =>
            {
                _eventList.Clear();
                EventCoordinator.SaveEvents(_eventList);

                _eventList = SortEvents(_eventList);
                RefreshEventRunners();
            });

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private ObservableCollection<Event> SortEvents(ObservableCollection<Event> eventList)
        {
            List<Event> sortedEventList = eventList
                        .OrderBy(item => Array.IndexOf(TimerFactory.Days, item.Day))
                        .ThenBy(item => Array.IndexOf(TimerFactory.Hours, item.Hour))
                        .ThenBy(item => Array.IndexOf(TimerFactory.Minutes, item.Minute)).ToList();
            eventList.Clear();
            foreach (var item in sortedEventList)
            {
                eventList.Add(item);
            }

            return eventList;
        }

        // Redraw Event Runner Timers
        public void RefreshEventRunners()
        {
            _eventRunners = TimerFactory.CreateTimers(_eventRunners, _eventList, runSpeedTest).ToList();
        }

        // Event Runners need to be refreshed when the environment goes to sleep, because the timers will stop counting in this time and throw the timinmg off
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                _eventRunners.Clear();
            }
            else if (e.Mode == PowerModes.Resume)
            {
                RefreshEventRunners();
            }
        }

        //private void SetupEventRunners()
        //{
        //    DayOfWeek day = DateTime.Now.DayOfWeek;
        //    _eventRunners.Clear();

        //    foreach (var e in _eventList)
        //    {
        //        DateTime current = DateTime.Now;
        //        Timer timer;

        //        int daysUntil = 0;
        //        int daysBetweenRunning = 7;
        //        if (e.Day == "Every day")
        //            daysBetweenRunning = 1;
        //        else
        //        {
        //            DayOfWeek dayToRun = DayOfWeek.Monday;
        //            switch (e.Day)
        //            {
        //                case "Monday": dayToRun = DayOfWeek.Monday; break;
        //                case "Tuesday": dayToRun = DayOfWeek.Tuesday; break;
        //                case "Wednesday": dayToRun = DayOfWeek.Wednesday; break;
        //                case "Thursday": dayToRun = DayOfWeek.Thursday; break;
        //                case "Friday": dayToRun = DayOfWeek.Friday; break;
        //                case "Saturday": dayToRun = DayOfWeek.Saturday; break;
        //                case "Sunday": dayToRun = DayOfWeek.Sunday; break;
        //            }
        //            daysUntil = ((int)dayToRun - (int)DateTime.Today.DayOfWeek + 7) % 7;
        //        }

        //        int hour = 0;
        //        if (e.Hour == "Midnight") hour = 0;
        //        else if (e.Hour == "Midday") hour = 12;
        //        else hour = int.Parse(e.Hour);

        //        int minute = int.Parse(e.Minute);

        //        int second = 0;
        //        if (hour == 0 && minute == 0)
        //        {
        //            hour = 23;
        //            minute = 59;
        //            second = 59;
        //        }

        //        TimeSpan timeToGo = (new TimeSpan(hour, minute, second) - current.TimeOfDay) + TimeSpan.FromDays(daysUntil);
        //        if (timeToGo < TimeSpan.Zero)
        //            timeToGo += TimeSpan.FromDays(daysBetweenRunning);

        //        if (timeToGo > TimeSpan.Zero)
        //        {
        //            timer = new System.Threading.Timer(x =>
        //            {
        //                Speedtester.RunSpeedTest();
        //            }, null, timeToGo, new TimeSpan(daysBetweenRunning, 0, 0, 0, 0));
        //            _eventRunners.Add(timer);
        //        }
        //    }
        //}
    }
}
