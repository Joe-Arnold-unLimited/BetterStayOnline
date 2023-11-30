using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;

namespace BetterStayOnline.MVVM.Model
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

        // Getters
        public static bool ShowMinDown() { return Get("ShowMinDown").ToLower() == "true"; }
        public static bool ShowMinUp() { return Get("ShowMinUp").ToLower() == "true"; }
        public static int MinDown() { return int.Parse(Get("MinDown")); }
        public static int MinUp() { return int.Parse(Get("MinUp")); }
        public static bool ShowDownloadRange() { return Get("ShowDownloadRange").ToLower() == "true"; }
        public static bool ShowUploadRange() { return Get("ShowUploadRange").ToLower() == "true"; }
        public static double DownloadRange() { return double.Parse(Get("DownloadRange")) / 100; }
        public static double UploadRange() { return double.Parse(Get("UploadRange")) / 100; }
        public static bool ShowPercentagesBelowMinimums() { return Get("ShowPercentagesBelowMinimums").ToLower() == "true"; }
        public static bool RunTestOnStartup() { return Get("RunTestOnStartup").ToLower() == "true"; }

        // Setters
        public static void SetShowMinDown(bool value) { Set("ShowMinDown", value); }
        public static void SetShowMinUp(bool value) { Set("ShowMinUp", value); }
        public static void SetMinDown(int value) { Set("MinDown", value); }
        public static void SetMinUp(int value) { Set("MinUp", value); }
        public static void SetShowDownloadRange(bool value) { Set("ShowDownloadRange", value); }
        public static void SetShowUploadRange(bool value) { Set("ShowUploadRange", value); }
        public static void SetDownloadRange(double value) { Set("DownloadRange", value); }
        public static void SetUploadRange(double value) { Set("UploadRange", value); }
        public static void SetShowPercentagesBelowMinimums(bool value) { Set("ShowPercentagesBelowMinimums", value); }
        public static void SetRunTestOnStartup(bool value) { Set("RunTestOnStartup", value); }
    }
}