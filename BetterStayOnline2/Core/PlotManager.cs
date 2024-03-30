using Newtonsoft.Json.Linq;
using OpenTK;
using ScottPlot;
using ScottPlot.AxisRules;
using ScottPlot.Colormaps;
using ScottPlot.DataSources;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static SkiaSharp.HarfBuzz.SKShaper;
using Color = ScottPlot.Color;

namespace BetterStayOnline2.Core
{
    public static class PlotManager
    {
        public static WpfPlot ResultsTable { get; set; } = new WpfPlot();

        static double[] downloadSpeeds = { };
        static double[] uploadSpeeds = { };
        static double[] datetimes = { };

        static PlotManager()
        {
            AddTestResults(ReadPreexistingData());
        }

        public async static void AddDatapoint(double down, double up, double time)
        {
            double[] newDownloadSpeeds = new double[downloadSpeeds.Length + 1];
            double[] newUploadSpeeds = new double[uploadSpeeds.Length + 1];
            double[] newDatetimes = new double[datetimes.Length + 1];

            for (int i = 0; i < downloadSpeeds.Length; i++)
            {
                newDownloadSpeeds[i] = downloadSpeeds[i];
                newUploadSpeeds[i] = uploadSpeeds[i];
                newDatetimes[i] = datetimes[i];
            }

            newDownloadSpeeds[downloadSpeeds.Length] = down;
            newUploadSpeeds[uploadSpeeds.Length] = up;
            newDatetimes[datetimes.Length] = time;

            downloadSpeeds = newDownloadSpeeds;
            uploadSpeeds = newUploadSpeeds;
            datetimes = newDatetimes;
        }

        // Update table with new data
        public static void UpdatePlot(WpfPlot table)
        {
            // TODO: Get from settings
            bool drawDownloadScatter = false;
            bool drawUploadScatter = false;
            bool drawDownloadTrendline = false;
            bool drawUploadTrendline = true;
            int trendByDays = 7;
            bool drawDownloadCandles = true;
            bool drawUploadCandles = false;
            string candleDays = "Daily";

            table.Plot.Clear();

            // Redraw monthlines as they've just been cleared
            int numberOfMonthsEitherSideToDraw = 24;
            DrawMonthLines(table, numberOfMonthsEitherSideToDraw);

            // Draw data
            // We draw upload first usually as download should be in front of upload
            // Draw upload trendline
            if (drawDownloadTrendline)
            {
                DrawTrendlines(table, trendByDays);
            }
            // Draw download trendline
            if (drawUploadTrendline)
            {
                DrawTrendlines(table, trendByDays, false);
            }
            // Draw upload scatter
            if (drawUploadScatter)
            {
                var uploadScatter = table.Plot.Add.Scatter(datetimes, uploadSpeeds, uploadLineColor);
                uploadScatter.MarkerStyle.Outline.Color = uploadLineColor;
            }
            // Draw download scatter
            if (drawDownloadScatter)
            {
                var downloadScatter = table.Plot.Add.Scatter(datetimes, downloadSpeeds, downloadLineColor);
                downloadScatter.MarkerStyle.Outline.Color = downloadLineColor;
            }
            // Show Upload Candles on top of Download candles because upload usually has smaller vertical range and will cover less
            // Draw Download Candles
            if (drawDownloadCandles)
            {
                DrawCandles(table, candleDays, downloadSpeeds);
            }
            // Draw Upload Candles
            if (drawUploadCandles)
            {
                DrawCandles(table, candleDays, uploadSpeeds, false);
            }

            SetXAxisStartingView(table);

            table.Refresh();
        }

        // Create table
        // Should only be called in one place, on table construction
        // Nowhere in here should we be drawing any plots, as they will be cleared immediately after
        public static void DrawTable(WpfPlot table)
        {
            SetXAxisStartingView(table);

            double yAxisTopLimit = SetYAxisStartingView(table);

            CreateYAxisTicks(table, yAxisTopLimit);

            LockVerticalZoom(table);

            AssignXAxisDateFormat(table);

            AssignPlotStyle(table);
        }

        #region Plottables

        // Month lines are created using Vertical Lines which are plots
        private static void DrawMonthLines(WpfPlot table, int numberOfMonthsEitherSideToDraw)
        {
            Alignment[] alignments = (Alignment[])Enum.GetValues(typeof(ScottPlot.Alignment));
            string[] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            if (datetimes.Length > 1)
            {
                var firstDate = DateTime.FromOADate(datetimes[0]).AddMonths(-numberOfMonthsEitherSideToDraw);

                var startMonthLines = DateTime.FromOADate(datetimes[0]).AddMonths(-numberOfMonthsEitherSideToDraw);
                var endMonthLines = DateTime.Now.AddMonths(numberOfMonthsEitherSideToDraw);

                for (DateTime month = new DateTime(startMonthLines.Year, startMonthLines.Month, 1); month < endMonthLines; month = month.AddMonths(1))
                {
                    var monthLine = table.Plot.Add.VerticalLine(x: month.ToOADate(), color: monthLineColor, width: 2);

                    string monthText = months[month.Month - 1].Substring(0, 3);
                    if (month.Year != DateTime.Now.Year) monthText = monthText + " " + month.Year.ToString().Substring(2);

                    monthLine.Text = monthText;
                    monthLine.Label.LineSpacing = 2;
                    monthLine.LabelOppositeAxis = true;
                    monthLine.Label.BackColor = new Color(0, 0, 0, 0);
                    monthLine.Label.Bold = false;
                    monthLine.Label.ForeColor = axisColor;
                }
            }
        }

        struct MovingAverage
        {
            DateTime date;
            double speed;
        }

        private static void DrawTrendlines(WpfPlot table, int trendByDays, bool download = true)
        {
            if (datetimes.Length == 0) return;

            var firstDate = DateTime.FromOADate(datetimes[0]);
            var lastDate = DateTime.Now;

            var speeds = download ? downloadSpeeds : uploadSpeeds;

            List<double> dates = new List<double>();
            List<double> averages = new List<double>();

            for (DateTime currentDate = firstDate; currentDate <= lastDate; currentDate = currentDate.AddDays(trendByDays))
            {
                List<int> indexesInRange = new List<int>();
                bool foundOne = false;
                for (int i = 0; i < datetimes.Length; i++)
                {
                    DateTime date = DateTime.FromOADate(datetimes[i]);
                    if (date > currentDate && date <= currentDate.AddDays(trendByDays))
                    {
                        indexesInRange.Add(i);
                        foundOne = true;
                    }
                    else
                    {
                        if (foundOne) break;
                    }
                }

                if (indexesInRange.Count == 0) continue;

                double[] speedsInRange = speeds.Skip(indexesInRange.Min()).Take(indexesInRange.Count).ToArray();

                dates.Add(currentDate.AddHours(trendByDays * 12).ToOADate());
                averages.Add(speedsInRange.Average());
            }

            var averageScatter = table.Plot.Add.Scatter(dates.ToArray(), averages.ToArray(), download ? downloadTrendlineColor : uploadTrendlineColor);
            averageScatter.Label = (download ? "Download" : "Upload") + " Trend";
            averageScatter.LineWidth = 8;
            averageScatter.Smooth = true;
            averageScatter.SmoothTension = 0;
            averageScatter.MarkerStyle = MarkerStyle.None;
        }

        private static void DrawCandles(WpfPlot table, string candlePeriod, double[] speeds, bool download = true)
        {
            if (datetimes.Length == 0) return;

            var firstDate = DateTime.FromOADate(datetimes[0]);
            var lastDate = DateTime.Now;

            var upperBoxPolygons = new List<Coordinates[]>();
            var lowerBoxPolygons = new List<Coordinates[]>();

            for (DateTime currentDate = candlePeriod == "Monthly" ? new DateTime(firstDate.Year, firstDate.Month, 1) : new DateTime(firstDate.Year, firstDate.Month, firstDate.Day);
            currentDate < lastDate;
            currentDate = candlePeriod == "Monthly" ? AddMonth(currentDate) : candlePeriod == "Weekly" ? currentDate.AddDays(7) : currentDate.AddDays(1))
            {
                DateTime endOfPeriodDate;
                switch (candlePeriod)
                {
                    case "Daily":
                        endOfPeriodDate = currentDate.AddDays(1);
                        break;
                    case "Weekly":
                        endOfPeriodDate = currentDate.AddDays(7);
                        break;
                    default:
                    case "Monthly":
                        endOfPeriodDate = AddMonth(currentDate);
                        break;
                }

                List<int> indexesInRange = new List<int>();
                bool foundOne = false;
                for (int i = 0; i < datetimes.Length; i++)
                {
                    DateTime date = DateTime.FromOADate(datetimes[i]);
                    if (date > currentDate && date <= endOfPeriodDate)
                    {
                        indexesInRange.Add(i);
                        foundOne = true;
                    }
                    else
                    {
                        if (foundOne) break;
                    }
                }

                if (indexesInRange.Count == 0) continue;

                double[] speedsInRange = speeds.Skip(indexesInRange.Min()).Take(indexesInRange.Count).ToArray();

                TimeSpan periodTimeSpan = endOfPeriodDate - currentDate;
                int hoursInPeriod = periodTimeSpan.Days * 24;
                int halfHours = hoursInPeriod / 2;
                double marginHours = halfHours * 0.2;

                DateTime startPoint = currentDate.AddHours(marginHours);
                DateTime endPoint = currentDate.AddHours(hoursInPeriod).AddHours(-marginHours);
                DateTime midPoint = currentDate.AddHours(halfHours);

                if (speedsInRange.Length > 2)
                {
                    // Calculate mean and standard deviation
                    CalculateMeanAndStdDev(speedsInRange.ToArray(), out double mean, out double leftStdDev, out double rightStdDev);

                    var upperDev = mean + leftStdDev;
                    var lowerDev = mean - rightStdDev;

                    // Add upper and lower box polygons to the plot
                    Coordinates[] upperPoly = new Coordinates[]
                    {
                        new Coordinates(startPoint.ToOADate(), mean),
                        new Coordinates(startPoint.ToOADate(), upperDev),
                        new Coordinates(endPoint.ToOADate(), upperDev),
                        new Coordinates(endPoint.ToOADate(), mean)
                    };
                    Coordinates[] lowerPoly = new Coordinates[]
                    {
                        new Coordinates(startPoint.ToOADate(), mean),
                        new Coordinates(startPoint.ToOADate(), lowerDev),
                        new Coordinates(endPoint.ToOADate(), lowerDev),
                        new Coordinates(endPoint.ToOADate(), mean)
                    };

                    upperBoxPolygons.Add(upperPoly);
                    lowerBoxPolygons.Add(lowerPoly);

                    LinePlot upLine = table.Plot.Add.Line(new Coordinates(midPoint.ToOADate(), mean + leftStdDev), new Coordinates(midPoint.ToOADate(), speedsInRange.Max()));
                    upLine.Color = download ? downloadUpLineColor : uploadUpLineColor;

                    LinePlot lowerLine = table.Plot.Add.Line(new Coordinates(midPoint.ToOADate(), mean - rightStdDev), new Coordinates(midPoint.ToOADate(), speedsInRange.Min()));
                    lowerLine.Color = download ? downloadDownLineColor : uploadDownLineColor;

                    upLine.LineWidth = candleLineWidth;
                    lowerLine.LineWidth = candleLineWidth;
                }
                else if (speedsInRange.Length == 2)
                {
                    var mean = CalculateMean(speedsInRange.ToArray());

                    LinePlot topVertLine = table.Plot.Add.Line(new Coordinates(midPoint.ToOADate(), speedsInRange.Max()), new Coordinates(midPoint.ToOADate(), mean));
                    topVertLine.Color = download ? downloadUpLineColor : uploadUpLineColor;

                    LinePlot midLine = table.Plot.Add.Line(new Coordinates(startPoint.ToOADate(), mean), new Coordinates(endPoint.ToOADate(), mean));
                    midLine.Color = download ? downloadDownLineColor : uploadDownLineColor;

                    LinePlot bottomVertLine = table.Plot.Add.Line(new Coordinates(midPoint.ToOADate(), mean), new Coordinates(midPoint.ToOADate(), speedsInRange.Min()));
                    bottomVertLine.Color = download ? downloadDownLineColor : uploadDownLineColor;

                    topVertLine.LineWidth = candleLineWidth;
                    midLine.LineWidth = candleLineWidth;
                    bottomVertLine.LineWidth = candleLineWidth;
                }
                else if (speedsInRange.Length == 1)
                {
                    LinePlot line = table.Plot.Add.Line(new Coordinates(startPoint.ToOADate(), speedsInRange[0]), new Coordinates(endPoint.ToOADate(), speedsInRange[0]));
                    line.Color = download ? downloadDownLineColor : uploadDownLineColor;

                    line.LineWidth = candleLineWidth;
                }
            }

            foreach (var polygon in upperBoxPolygons)
            {
                Polygon poly = table.Plot.Add.Polygon(polygon);
                poly.FillStyle.Color = download ? downloadUpCandleColor : uploadUpCandleColor;
                poly.LineStyle.Color = download ? downloadUpLineColor : uploadUpLineColor;
                poly.LineStyle.Width = 0;
                poly.MarkerStyle.Outline.Color = new Color(0, 0, 0, 0);
            }
            foreach (var polygon in lowerBoxPolygons)
            {
                Polygon poly = table.Plot.Add.Polygon(polygon);
                poly.FillStyle.Color = download ? downloadDownCandleColor : uploadDownCandleColor;
                poly.LineStyle.Color = download ? downloadDownLineColor : uploadDownLineColor;
                poly.LineStyle.Width = 0;
                poly.MarkerStyle.Outline.Color = new Color(0, 0, 0, 0);
            }
        }

        #endregion

        #region Data Handling

        public struct BandwidthTest
        {
            public DateTime date;
            public double downSpeed;
            public double upSpeed;
        }

        private static List<BandwidthTest> ReadPreexistingData()
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

        private async static void AddTestResults(List<BandwidthTest> testResults)
        {
            foreach (var result in testResults)
            {
                AddTestResult(result);
            }
        }

        private async static void AddTestResult(BandwidthTest result)
        {
            AddDatapoint(result.downSpeed, result.upSpeed, result.date.ToOADate());
        }

        #endregion

        #region Table Drawing

        private static void SetXAxisStartingView(WpfPlot table)
        {
            bool moreThan31DaysOfResults = false;

            if (datetimes.Length > 0 && DateTime.FromOADate(datetimes.Min()) < DateTime.Now.AddDays(-31)) moreThan31DaysOfResults = true;

            if (datetimes.Length == 0)
                table.Plot.Axes.SetLimitsX(DateTime.Now.AddDays(-1).ToOADate(), DateTime.Now.AddDays(1).ToOADate());
            else if (moreThan31DaysOfResults || datetimes.Length == 1)
                table.Plot.Axes.SetLimitsX(DateTime.Now.AddDays(-34).ToOADate(), DateTime.FromOADate(datetimes.Max()).AddDays(3).ToOADate());
            else
            {
                double lengthOfTests = (DateTime.FromOADate(datetimes.Max()) - DateTime.FromOADate(datetimes.Min())).TotalMinutes;
                if (lengthOfTests < TimeSpan.FromHours(2).TotalMinutes)
                {
                    table.Plot.Axes.SetLimitsX(DateTime.FromOADate(datetimes.Min()).AddHours(-1).ToOADate(),
                        DateTime.FromOADate(datetimes.Max()).AddHours(1).ToOADate());
                }
                else
                {
                    table.Plot.Axes.SetLimitsX(DateTime.FromOADate(datetimes.Min()).AddMinutes(-(lengthOfTests / 5)).ToOADate(),
                        DateTime.FromOADate(datetimes.Max()).AddMinutes(lengthOfTests / 5).ToOADate());
                }
            }
        }

        private static double SetYAxisStartingView(WpfPlot table)
        {
            double yAxisTopLimit = 100;

            if (downloadSpeeds.Length > 0)
            {
                var highestYValue = downloadSpeeds.Concat(uploadSpeeds).Max();
                yAxisTopLimit = highestYValue + 10 - (highestYValue % 10);
            }
            table.Plot.Axes.SetLimitsY(0, yAxisTopLimit);

            return yAxisTopLimit;
        }

        private static void CreateYAxisTicks(WpfPlot table, double yAxisTopLimit)
        {
            NumericManual ticks = new NumericManual();

            double multiplesOfTenInYAxis = yAxisTopLimit / 10;
            int majorTickPoints = 5;
            int minorTickPoints = 1;
            if (multiplesOfTenInYAxis < 7)
                majorTickPoints = 5;
            else if (multiplesOfTenInYAxis < 15)
                majorTickPoints = 10;
            else if (multiplesOfTenInYAxis < 30)
                majorTickPoints = 20;
            else if (multiplesOfTenInYAxis < 40)
                majorTickPoints = 25;
            else if (multiplesOfTenInYAxis < 100)
                majorTickPoints = 50;
            else
                majorTickPoints = 100;
            minorTickPoints = majorTickPoints / 5;

            for (int i = 0; i <= yAxisTopLimit; i += majorTickPoints)
            {
                ticks.AddMajor(i, i.ToString());
            }
            for (int i = 0; i <= yAxisTopLimit; i += minorTickPoints)
            {
                if (i % majorTickPoints == 0) continue;
                ticks.AddMinor(i);
            }

            table.Plot.Axes.Left.TickGenerator = ticks;
        }

        private static void LockVerticalZoom(WpfPlot table)
        {
            LockedVertical lockedVerticalRule = new LockedVertical(table.Plot.Axes.Left);

            table.Plot.Axes.Rules.Clear();
            table.Plot.Axes.Rules.Add(lockedVerticalRule);
        }

        private static void AssignXAxisDateFormat(WpfPlot table)
        {
            table.Plot.Axes.DateTimeTicksBottom();
        }

        #endregion

        #region Styles

        private static Color outerGraphColor = new Color(0, 0, 0, 0);
        private static Color graphBackgroundColor = new Color(25, 24, 24);
        private static Color axisColor = new Color(153, 170, 181);
        private static Color majorLineColor = new Color(38, 39, 41);
        private static Color monthLineColor = new Color(51, 50, 50);

        private static Color downloadLineColor = new Color(100, 149, 237);
        private static Color uploadLineColor = new Color(255, 69, 0);

        private static Color minDownloadColor = new Color(0, 191, 255);
        private static Color minUploadColor = new Color(255, 165, 0);

        private static Color downloadTrendlineColor = new Color(72, 61, 139);
        private static Color uploadTrendlineColor = new Color(165, 42, 42);

        private static Color downloadUpCandleColor = new Color(18, 153, 113);
        private static Color downloadUpLineColor = new Color(18, 153, 113);

        private static Color downloadDownCandleColor = new Color(29, 99, 168);
        private static Color downloadDownLineColor = new Color(29, 99, 168);

        private static Color uploadUpCandleColor = new Color(217, 127, 43);
        private static Color uploadUpLineColor = new Color(217, 127, 43);

        private static Color uploadDownCandleColor = new Color(176, 56, 64);
        private static Color uploadDownLineColor = new Color(176, 56, 64);

        private static int candleLineWidth = 2;

        private static void AssignPlotStyle(WpfPlot table)
        {
            table.Plot.FigureBackground.Color = outerGraphColor;
            table.Plot.DataBackground.Color = graphBackgroundColor;
            table.Plot.Axes.Color(axisColor);
            table.Plot.Grid.MajorLineColor = majorLineColor;

            Fonts.Default = "Aptos";

            table.Plot.Axes.Bottom.TickLabelStyle.FontName = Fonts.Default;

            table.Plot.Font.Set(Fonts.Default);
            table.Plot.Axes.Bottom.TickLabelStyle.FontSize = 13;

            table.Plot.Axes.FrameColor(new Color(0, 0, 0, 0));
        }

        #endregion

        #region Functions

        private static DateTime AddMonth(DateTime date)
        {
            if (date.Month == 12)
            {
                return new DateTime(date.Year + 1, 1, date.Day, date.Hour, date.Minute, date.Second);
            }

            return new DateTime(date.Year, date.Month + 1, date.Day, date.Hour, date.Minute, date.Second);
        }

        static void CalculateMeanAndStdDev(double[] array, out double mean, out double leftStdDev, out double rightStdDev)
        {
            array = array.OrderByDescending(val => val).ToArray();

            int index = array.Length / 2;

            mean = CalculateMean(array);

            double[] leftArray = new double[index];
            Array.Copy(array, 0, leftArray, 0, index);
            leftStdDev = CalculateStdDev(leftArray);

            double[] rightArray = new double[array.Length - index - 1];
            Array.Copy(array, index + 1, rightArray, 0, array.Length - index - 1);
            rightStdDev = CalculateStdDev(rightArray);
        }

        static double CalculateMean(double[] array)
        {
            return array.Sum() / array.Length;
        }

        static double CalculateStdDev(double[] array)
        {
            double mean = CalculateMean(array);
            double sumSquaredDiff = 0.0;
            foreach (var number in array)
            {
                sumSquaredDiff += Math.Pow(number - mean, 2);
            }
            return Math.Sqrt(sumSquaredDiff / array.Length);
        }

        #endregion
    }
}