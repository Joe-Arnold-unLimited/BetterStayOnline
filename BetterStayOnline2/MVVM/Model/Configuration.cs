using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using ScottPlot;
using ScottPlot.Plottable;

namespace BetterStayOnline2.MVVM.Model
{
    class Configuration
    {
        private static string path = AppDomain.CurrentDomain.BaseDirectory + "\\External\\config.json";

        private static JObject RemoveExistingKey(string key)
        {
            string data = File.ReadAllText(path);
            JObject obj = JObject.Parse(data);

            if (obj.ContainsKey(key)) obj.Remove(key);
            return obj;
        }

        private static void SaveConfigurationObject(JObject obj)
        {
            File.WriteAllText(path, obj.ToString());
        }

        private static string Get(string key)
        {
            string data = File.ReadAllText(path);
            JObject obj = JObject.Parse(data);

            if (obj.ContainsKey(key)) return obj.GetValue(key).ToString();

            return null;
        }

        private static void Set(string key, string value)
        {
            JObject obj = RemoveExistingKey(key);
            obj.Add(key, value);
            SaveConfigurationObject(obj);
        }

        private static void Set(string key, int value)
        {
            JObject obj = RemoveExistingKey(key);
            obj.Add(key, value);
            SaveConfigurationObject(obj);
        }

        private static void Set(string key, bool value)
        {
            JObject obj = RemoveExistingKey(key);
            obj.Add(key, value);
            SaveConfigurationObject(obj);
        }

        private static void Set(string key, double value)
        {
            JObject obj = RemoveExistingKey(key);
            obj.Add(key, value);
            SaveConfigurationObject(obj);
        }

        private static string[] candlePeriods = { "Monthly", "Weekly" };

        // Getters
        public static bool ShowDownloadPoints() { return Get("ShowDownloadPoints").ToLower() == "true"; }
        public static bool ShowUploadPoints() { return Get("ShowUploadPoints").ToLower() == "true"; }
        public static bool ShowDownloadTrendline() { return Get("ShowDownloadTrendline").ToLower() == "true"; }
        public static bool ShowUploadTrendline() { return Get("ShowUploadTrendline").ToLower() == "true"; }
        public static int DaysForAverage() { return int.Parse(Get("DaysForAverage")); }
        public static bool ShowMinDown() { return Get("ShowMinDown").ToLower() == "true"; }
        public static bool ShowMinUp() { return Get("ShowMinUp").ToLower() == "true"; }
        public static int MinDown() { return int.Parse(Get("MinDown")); }
        public static int MinUp() { return int.Parse(Get("MinUp")); }
        public static bool ShowDownloadCandles() { return Get("ShowDownloadCandles").ToLower() == "true"; }
        public static bool ShowUploadCandles() { return Get("ShowUploadCandles").ToLower() == "true"; }
        public static string CandlePeriod() { return Get("CandlePeriod"); }
        public static bool ShowPercentagesBelowMinimums() { return Get("ShowPercentagesBelowMinimums").ToLower() == "true"; }
        public static bool RunTestOnStartup() { return Get("RunTestOnStartup").ToLower() == "true"; }


        // Setters
        public static void SetShowDownloadPoints(bool value) { Set("ShowDownloadPoints", value); }
        public static void SetShowUploadPoints(bool value) { Set("ShowUploadPoints", value); }
        public static void SetShowDownloadTrendline(bool value) { Set("ShowDownloadTrendline", value); }
        public static void SetShowUploadTrendline(bool value) { Set("ShowUploadTrendline", value); }
        public static void SetDaysForAverage(int value) { Set("DaysForAverage", value); }
        public static void SetShowMinDown(bool value) { Set("ShowMinDown", value); }
        public static void SetShowMinUp(bool value) { Set("ShowMinUp", value); }
        public static void SetMinDown(int value) { Set("MinDown", value); }
        public static void SetMinUp(int value) { Set("MinUp", value); }
        public static void SetShowDownloadCandles(bool value) { Set("ShowDownloadCandles", value); }
        public static void SetShowUploadCandles(bool value) { Set("ShowUploadCandles", value); }
        public static void SetCandlePeriod(string value) { Set("CandlePeriod", value); }
        public static void SetShowPercentagesBelowMinimums(bool value) { Set("ShowPercentagesBelowMinimums", value); }
        public static void SetRunTestOnStartup(bool value) { Set("RunTestOnStartup", value); }
    }
}