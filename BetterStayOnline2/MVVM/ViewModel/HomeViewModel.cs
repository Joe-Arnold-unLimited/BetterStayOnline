using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using SkiaSharp;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using LiveChartsCore.SkiaSharpView.SKCharts;
using System.Drawing;

namespace BetterStayOnline2.MVVM.ViewModel
{
    internal class HomeViewModel
    {
        ObservableCollection<DateTimePoint> downloadPoints = new ObservableCollection<DateTimePoint>();
        ObservableCollection<DateTimePoint> uploadPoints = new ObservableCollection<DateTimePoint>();

        public Axis[] XAxes { get; set; } =
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMMM dd"))
        };

        public ISeries[] Series { get; set; } = new ISeries[]
        {
            new ScatterSeries<DateTimePoint>
            {
                Stroke = new SolidColorPaint(new SKColor(64, 95, 237)) { StrokeThickness = 1 },
                Fill = null,
                GeometrySize = 4
            },
            new ScatterSeries<DateTimePoint>
            {
                Stroke = new SolidColorPaint(new SKColor(255, 45, 00)) { StrokeThickness = 1 },
                Fill = null,
                GeometrySize = 4
            },
        };

        public ScatterSeries<ObservablePoint> upSeries { get; set; }

        public HomeViewModel()
        {
            AddNewTestResults(ReadPreexistingData());

            Series[0].Values = downloadPoints;
            Series[1].Values = uploadPoints;
        }

        #region Data Handling

        public struct BandwidthTest
        {
            public DateTime date;
            public double downSpeed;
            public double upSpeed;
        }

        private List<BandwidthTest> ReadPreexistingData()
        {
            JArray jsonResults = new JArray();
            List<BandwidthTest> testResults = new List<BandwidthTest>();

            try
            {
                string data = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\External\\testresults.json");
                JObject obj = JObject.Parse(data);
                if (obj.ContainsKey("TestResults"))
                {
                    jsonResults = (JArray)obj.GetValue("TestResults");

                    testResults.Clear();
                    foreach (var result in jsonResults)
                    {
                        BandwidthTest newTest = new BandwidthTest();
                        newTest.date = DateTime.Parse((string)((JObject)result).GetValue("DateTime"));
                        newTest.downSpeed = double.Parse((string)((JObject)result).GetValue("Download"));
                        newTest.upSpeed = double.Parse((string)((JObject)result).GetValue("Upload"));
                        testResults.Add(newTest);
                    }
                }
            }
            catch (Exception)
            {
                int x = 0;
            }

            return testResults;
        }

        private void AddNewTestResults(List<BandwidthTest> testResults)
        {
            foreach(var result in testResults)
            {
                downloadPoints.Add(new DateTimePoint(result.date, result.downSpeed));
                uploadPoints.Add(new DateTimePoint(result.date, result.upSpeed));
            }
        }

        #endregion
    }
}