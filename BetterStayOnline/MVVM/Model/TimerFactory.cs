﻿using BetterStayOnline.MVVM.View;
using BetterStayOnline.SpeedTest;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;

namespace BetterStayOnline.MVVM.Model
{
    static class TimerFactory
    {
        public static readonly string[] Days = new string[] { "Every day", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        public static readonly string[] Hours = new string[] { "Midnight", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "Midday", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
        public static readonly string[] Minutes = new string[] { "00", "15", "30", "45" };

        private static int CalcHour(Event e)
        {
            int hour;
            if (e.Hour == "Midnight") hour = 0;
            else if (e.Hour == "Midday") hour = 12;
            else hour = int.Parse(e.Hour);
            return hour;
        }

        private static int CalcMinute(Event e)
        {
            return int.Parse(e.Minute);
        }

        private static int CalcDaysBetween(Event e)
        {
            int daysBetweenRunning = 7;
            if (e.Day == "Every day")
                daysBetweenRunning = 1;
            return daysBetweenRunning;
        }

        private static TimeSpan TimeUntil(Event e)
        {
            DateTime current = DateTime.Now;
            int daysUntil = 0;

            int daysBetweenRunning = CalcDaysBetween(e);
            if (daysBetweenRunning != 1)
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

            int hour = CalcHour(e);
            int minute = CalcMinute(e);

            TimeSpan timeToGo = (new TimeSpan(hour, minute, 0) - current.TimeOfDay) + TimeSpan.FromDays(daysUntil);
            if (timeToGo < TimeSpan.Zero)
                timeToGo += TimeSpan.FromDays(daysBetweenRunning);

            return timeToGo;
        }

        public static ICollection<Timer> CreateTimers(ICollection<Timer> timers, IEnumerable<Event> events, Func<bool> job)
        {
            foreach (var timer in timers)
                timer.Dispose();
            timers.Clear();

            foreach (var e in events)
            {
                TimeSpan timeToGo = TimeUntil(e);

                if (timeToGo > TimeSpan.Zero)
                {
                    Timer timer = new Timer(x =>
                    {
                        job();
                    }, null, timeToGo, new TimeSpan(CalcDaysBetween(e), 0, 0, 0, 0));
                    timers.Add(timer);
                }
            }

            return timers;
        }

        public static ICollection<Timer> CreateTimers(ICollection<Timer> timers, IEnumerable<Event> events, Func<ResultsView, bool> job, ResultsView resultsView)
        {
            foreach(var timer in timers)
                timer.Dispose();
            timers.Clear();

            foreach (var e in events)
            {
                TimeSpan timeToGo = TimeUntil(e);
                timeToGo += TimeSpan.FromMinutes(1);

                if (timeToGo > TimeSpan.Zero)
                {
                    Timer timer = new Timer(x =>
                    {
                        job(resultsView);
                    }, null, timeToGo, new TimeSpan(CalcDaysBetween(e), 0, 0, 0, 0));
                    timers.Add(timer);
                }
            }

            return timers;
        }
    }
}
