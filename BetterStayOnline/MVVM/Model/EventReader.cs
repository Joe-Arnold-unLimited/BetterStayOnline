using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BetterStayOnline.MVVM.Model
{
    class EventReader
    {
        private static string path = AppDomain.CurrentDomain.BaseDirectory + "\\External\\events.json";

        public static ObservableCollection<Event> GetEvents()
        {
            ObservableCollection<Event> events = new ObservableCollection<Event>();

            JObject obj = JObject.Parse(File.ReadAllText(path));
            if (obj.ContainsKey("Events"))
            {
                foreach (JObject ev in (JArray)(obj.GetValue("Events")))
                {
                    Event e = new Event()
                    {
                        Day = (string)ev.GetValue("Day"),
                        Hour = (string)ev.GetValue("Hour"),
                        Minute = (string)ev.GetValue("Minute"),
                        Selected = (bool)ev.GetValue("Selected")
                    };

                    events.Add(e);
                }
            }

            return events;
        }

        public static void AddEvent(Event e)
        {
            ObservableCollection<Event> events = GetEvents();
            events.Add(e);
            SaveEvents(events);
        }

        public static void SaveEvents(ObservableCollection<Event> events)
        {
            JArray arr = new JArray();

            foreach (Event e in events)
            {
                JObject obj = new JObject
                {
                    { "Day", e.Day },
                    { "Hour", e.Hour },
                    { "Minute", e.Minute },
                    { "Selected", e.Selected }
                };
                arr.Add(obj);
            }

            JObject eventsObj = new JObject();
            eventsObj.Add("Events", arr);
            File.WriteAllText(path, eventsObj.ToString());
        }

        public static void RemoveSelected()
        {
            ObservableCollection<Event> events = new ObservableCollection<Event>();
            foreach (var e in GetEvents().Where(e => !e.Selected).ToList())
                events.Add(e);
            SaveEvents(events);
        }
    }
}