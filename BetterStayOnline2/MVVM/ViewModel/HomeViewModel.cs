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
using LiveChartsCore.Drawing;
using System.Windows.Controls.Primitives;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.Measure;

namespace BetterStayOnline2.MVVM.ViewModel
{
    internal class HomeViewModel
    {
        ObservableCollection<DateTimePoint> downloadPoints = new ObservableCollection<DateTimePoint>();
        ObservableCollection<DateTimePoint> uploadPoints = new ObservableCollection<DateTimePoint>();

        public Axis[] ScrollableAxes { get; set; } =
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString())
        };

        public RectangularSection[] Thumbs { get; set; }

        public ISeries[] Series { get; set; } = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Stroke = new SolidColorPaint(new SKColor(64, 95, 237)) { StrokeThickness = 2 },
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                DataPadding = new LvcPoint(0, 1),
                XToolTipLabelFormatter = null,
                YToolTipLabelFormatter = null,
            },
            new LineSeries<DateTimePoint>
            {
                Stroke = new SolidColorPaint(new SKColor(255, 45, 00)) { StrokeThickness = 2 },
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                DataPadding = new LvcPoint(0, 1),
                XToolTipLabelFormatter = null,
                YToolTipLabelFormatter = null
            },
        };

        public HomeViewModel()
        {
            AddTestResults(ReadPreexistingData());

            Series[0].Values = downloadPoints;
            Series[1].Values = uploadPoints;

            ScrollableAxes = new[] { new Axis() };

            Thumbs = new[]
            {
                new RectangularSection
                {
                    Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100))
                }
            };
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

        private void AddTestResults(List<BandwidthTest> testResults)
        {
            foreach(var result in testResults)
            {
                AddTestResult(result);
            }
        }

        private void AddTestResult(BandwidthTest result)
        {
            downloadPoints.Add(new DateTimePoint(result.date, result.downSpeed));
            uploadPoints.Add(new DateTimePoint(result.date, result.upSpeed));
        }

        #endregion

        #region Scrolling

        private bool _isDown = false;

        [RelayCommand]
        public void ChartUpdated(ChartCommandArgs args)
        {
            var cartesianChart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;

            var x = cartesianChart.XAxes.First();

            // update the scroll bar thumb when the chart is updated (zoom/pan)
            // this will let the user know the current visible range
            var thumb = Thumbs[0];

            thumb.Xi = x.MinLimit;
            thumb.Xj = x.MaxLimit;
        }

        [RelayCommand]
        public void PointerDown(PointerCommandArgs args)
        {
            _isDown = true;
        }

        [RelayCommand]
        public void PointerMove(PointerCommandArgs args)
        {
            if (!_isDown) return;

            var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
            var positionInData = chart.ScalePixelsToData(args.PointerPosition);

            var thumb = Thumbs[0];
            var currentRange = thumb.Xj - thumb.Xi;

            // update the scroll bar thumb when the user is dragging the chart
            thumb.Xi = positionInData.X - currentRange / 2;
            thumb.Xj = positionInData.X + currentRange / 2;

            // update the chart visible range
            ScrollableAxes[0].MinLimit = thumb.Xi;
            ScrollableAxes[0].MaxLimit = thumb.Xj;
        }

        [RelayCommand]
        public void PointerUp(PointerCommandArgs args)
        {
            _isDown = false;
        }

        #endregion
    }
}