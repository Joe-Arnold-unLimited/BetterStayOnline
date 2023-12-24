using BetterStayOnline.MVVM.Model;
using Newtonsoft.Json.Linq;
using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline.Model
{
    public class PlotManager
    {
        private static PlotManager _secondWindow;
        public static PlotManager SecondWindow
        {
            get
            {
                if (_secondWindow == null)
                {
                    _secondWindow = new PlotManager();
                }
                return _secondWindow;
            }
            set
            {
                _secondWindow = value;
            }
        }

        private static JArray jsonTestResults = new JArray();
        public static List<BandwidthTest> testResults = new List<BandwidthTest>();

        public WpfPlotViewer viewer;
        public WpfPlot wpfPlot;
        public Plot plot;

        public static int countBelowMinDownload = 0;
        public static int countBelowMinUpload = 0;

        public event Action<Plot, WpfPlot, BandwidthTest> OnPlotUpdate;
        public bool showDownload;
        public bool showUpload;
        public bool showDownloadTrendline;
        public bool showUploadTrendline;
        public bool showMinDownload;
        public bool showMinUpload;
        public bool showDownloadCandles;
        public bool showUploadCandles;
        public int minimumDownload;
        public int minimumUpload;

        private bool _firstTime = true;
        public bool firstTime
        {
            get
            {
                if (_firstTime)
                {
                    _firstTime = false;
                    return true;
                }
                return _firstTime;
            }
        }

        public PlotManager(Plot plot, bool showDownload, bool showUpload, bool showDownloadTrendline, bool showUploadTrendline, bool showMinDownload, bool showMinUpload, bool showDownloadCandles, bool showUploadCandles, int minimumDownload, int minimumUpload, WpfPlot wpfPlot  = null) : this(wpfPlot)
        {
            this.plot = plot;
            this.showDownload = showDownload;
            this.showUpload = showUpload;
            this.showDownloadTrendline = showDownloadTrendline;
            this.showUploadTrendline = showUploadTrendline;
            this.showMinDownload = showMinDownload;
            this.showMinUpload = showMinUpload;
            this.showDownloadCandles = showDownloadCandles;
            this.showUploadCandles = showUploadCandles;
            this.minimumDownload = minimumDownload;
            this.minimumUpload = minimumUpload;
        }

        public ScatterPlotList<double> downloadScatter;
        public ScatterPlotList<double> uploadScatter;
        public ScatterPlotList<double> downloadTrendlineScatter;
        public ScatterPlotList<double> uploadTrendlineScatter;
        public HLine downloadHLineVector;
        public HLine uploadHLineVector;

        public List<List<(double x, double y)>> uploadUpperBoxPolygons;
        public List<List<(double x, double y)>> uploadLowerBoxPolygons;
        public List<List<(double x, double y)>> downloadUpperBoxPolygons;
        public List<List<(double x, double y)>> downloadLowerBoxPolygons;

        public Polygons downloadUpperPolygons;
        public Polygons downloadLowerPolygons;
        public List<ScatterPlot> downloadUpperLines;
        public List<ScatterPlot> downloadLowerLines;
        public Polygons uploadUpperPolygons;
        public Polygons uploadLowerPolygons;
        public List<ScatterPlot> uploadUpperLines;
        public List<ScatterPlot> uploadLowerLines;

        // Styling
        // TODO: Move to settings
        Color graphBackgroundColor;

        Color downloadTrendlineColor;
        Color downloadLineColor;
        Color minDownloadColor;
        Color uploadTrendlineColor;
        Color uploadLineColor;
        Color minUploadColor;

        Color downloadUpCandleColor;
        Color downloadDownCandleColor;
        Color downloadUpLineColor;
        Color downloadDownLineColor;

        Color uploadUpCandleColor;
        Color uploadDownCandleColor;
        Color uploadUpLineColor;
        Color uploadDownLineColor;

        private int candleLineWidth = 1;

        private PlotManager(WpfPlot wpfPlot = null)
        {
            graphBackgroundColor = Color.FromArgb(7, 38, 59);

            downloadTrendlineColor = Color.DarkSlateBlue;
            downloadLineColor = Color.CornflowerBlue;
            minDownloadColor = Color.DeepSkyBlue;
            uploadTrendlineColor = Color.Brown;
            uploadLineColor = Color.OrangeRed;
            minUploadColor = Color.Orange;

            downloadUpCandleColor = Color.FromArgb(75, Color.Aqua);
            downloadDownCandleColor = Color.FromArgb(75, Color.DodgerBlue);
            downloadUpLineColor = Color.FromArgb(255, (int)(0.80 * downloadUpCandleColor.R), (int)(0.80 * downloadUpCandleColor.G), (int)(0.80 * downloadUpCandleColor.B));
            downloadDownLineColor = Color.FromArgb(255, (int)(0.80 * downloadDownCandleColor.R), (int)(0.80 * downloadDownCandleColor.G), (int)(0.80 * downloadDownCandleColor.B));

            uploadUpCandleColor = Color.FromArgb(75, Color.Orange);
            uploadDownCandleColor = Color.FromArgb(75, Color.Red);
            uploadUpLineColor = Color.FromArgb(255, (int)(0.80 * uploadUpCandleColor.R), (int)(0.80 * uploadUpCandleColor.G), (int)(0.80 * uploadUpCandleColor.B));
            uploadDownLineColor = Color.FromArgb(255, (int)(0.80 * uploadDownCandleColor.R), (int)(0.80 * uploadDownCandleColor.G), (int)(0.80 * uploadDownCandleColor.B));

            ReadPreexistingData();

            if (wpfPlot != null)
            {
                // in window graph
                this.wpfPlot = wpfPlot;
            }
        }

        private void ReadPreexistingData()
        {
            try
            {
                string data = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\External\\testresults.json");
                JObject obj = JObject.Parse(data);
                if (obj.ContainsKey("TestResults"))
                {
                    jsonTestResults = (JArray)obj.GetValue("TestResults");

                    testResults.Clear();
                    foreach (var result in jsonTestResults)
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
        }

        bool firstUpdate = true;

        public void UpdatePlot()
        {
            DrawViewer(null);

            if (viewer != null)
                viewer.Dispatcher.Invoke(() =>
                {
                    plot.Render();
                    viewer.wpfPlot1.Refresh();
                });
            if (wpfPlot != null) 
                wpfPlot.Dispatcher.Invoke(() =>
                {
                    wpfPlot.Render();
                });
            if (plot != null)
                plot.Legend();

            // Notify subscribers that the plot has been updated
            OnPlotUpdate?.Invoke(plot, wpfPlot, testResults.Count() > 0 ? testResults.Last() : new BandwidthTest());
        }

        private void DrawCandles(bool download)
        {
            var firstDate = testResults.First().date;
            var lastDate = DateTime.Now;
            string candlePeriod = Configuration.CandlePeriod();

            var upperLines = new List<ScatterPlot>();
            var lowerLines = new List<ScatterPlot>();

            var upperBoxPolygons = new List<List<(double x, double y)>>();
            var lowerBoxPolygons = new List<List<(double x, double y)>>();

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

                var speedsInRange = testResults
                    .Where(result => result.date > currentDate && result.date <= endOfPeriodDate).ToList()
                    .Select(result => download ? result.downSpeed : result.upSpeed)
                    .ToList();

                TimeSpan periodTimeSpan = endOfPeriodDate - currentDate;
                int hoursInPeriod = periodTimeSpan.Days * 24;
                int halfHours = hoursInPeriod / 2;
                double marginHours = halfHours * 0.2;

                DateTime startPoint = currentDate.AddHours(marginHours);
                DateTime endPoint = currentDate.AddHours(hoursInPeriod).AddHours(-marginHours);
                DateTime midPoint = currentDate.AddHours(halfHours);

                if (speedsInRange.Count > 2)
                {
                    // Calculate mean and standard deviation
                    CalculateMeanAndStdDev(speedsInRange.ToArray(), out double mean, out double leftStdDev, out double rightStdDev);

                    var upperDev = mean + leftStdDev;
                    var lowerDev = mean - rightStdDev;

                    // Calculate candlestick coordinates
                    double[] xs = { startPoint.ToOADate(), endPoint.ToOADate(), endPoint.ToOADate(), startPoint.ToOADate() };
                    double[] upperBoxYs = { mean + leftStdDev, mean + leftStdDev, mean, mean };
                    double[] lowerBoxYs = { mean - rightStdDev, mean - rightStdDev, mean, mean };

                    // Add upper and lower box polygons to the plot
                    List<(double x, double y)> upperBoxPolygon = xs.Zip(upperBoxYs, (xp, yp) => (xp, yp)).ToList();
                    List<(double x, double y)> lowerBoxPolygon = xs.Zip(lowerBoxYs, (xp, yp) => (xp, yp)).ToList();
                    upperBoxPolygons.Add(upperBoxPolygon);
                    lowerBoxPolygons.Add(lowerBoxPolygon);

                    ScatterPlot upLine = plot.AddLine(midPoint.ToOADate(), mean + leftStdDev, midPoint.ToOADate(), speedsInRange.Max(), download ? downloadUpLineColor : uploadUpLineColor);
                    ScatterPlot lowerLine = plot.AddLine(midPoint.ToOADate(), mean - rightStdDev, midPoint.ToOADate(), speedsInRange.Min(), download ? downloadDownLineColor : uploadDownLineColor);

                    upLine.LineWidth = candleLineWidth;
                    lowerLine.LineWidth = candleLineWidth;

                    upperLines.Add(upLine);
                    lowerLines.Add(lowerLine);
                }
                else if (speedsInRange.Count == 2)
                {
                    var mean = CalculateMean(speedsInRange.ToArray());

                    ScatterPlot topVertLine = plot.AddLine(midPoint.ToOADate(), speedsInRange.Max(), midPoint.ToOADate(), mean, download ? downloadUpLineColor : uploadUpLineColor);
                    ScatterPlot midLine = plot.AddLine(startPoint.ToOADate(), mean, endPoint.ToOADate(), mean, download ? downloadDownLineColor : uploadDownLineColor);
                    ScatterPlot bottomVertLine = plot.AddLine(midPoint.ToOADate(), mean, midPoint.ToOADate(), speedsInRange.Min(), download ? downloadDownLineColor : uploadDownLineColor);

                    topVertLine.LineWidth = candleLineWidth;
                    midLine.LineWidth = candleLineWidth;
                    bottomVertLine.LineWidth = candleLineWidth;
                }
                else if (speedsInRange.Count == 1)
                {
                    ScatterPlot line = plot.AddLine(startPoint.ToOADate(), speedsInRange[0], endPoint.ToOADate(), speedsInRange[0], download ? downloadDownLineColor : uploadDownLineColor);
                    line.LineWidth = candleLineWidth;
                    upperLines.Add(line);
                }
            }

            var upperPolygons = plot.AddPolygons(upperBoxPolygons, fillColor: download ? downloadUpCandleColor : uploadUpCandleColor, lineColor: download ? downloadUpLineColor : uploadUpLineColor, lineWidth: candleLineWidth);
            var lowerPolygons = plot.AddPolygons(lowerBoxPolygons, fillColor: download ? downloadDownCandleColor : uploadDownCandleColor, lineColor: download ? downloadDownLineColor : uploadDownLineColor, lineWidth: candleLineWidth);

            if (download)
            {
                downloadUpperLines = upperLines;
                downloadLowerLines = lowerLines;
                downloadUpperBoxPolygons = upperBoxPolygons;
                downloadLowerBoxPolygons = lowerBoxPolygons;
                downloadUpperPolygons = upperPolygons;
                downloadLowerPolygons = lowerPolygons;
            }
            else
            {
                uploadUpperLines = upperLines;
                uploadLowerLines = lowerLines;
                uploadUpperBoxPolygons = upperBoxPolygons;
                uploadLowerBoxPolygons = lowerBoxPolygons;
                uploadUpperPolygons = upperPolygons;
                uploadLowerPolygons = lowerPolygons;
            }
        }

        public void AddResult(BandwidthTest testResult, bool render = false)
        {
            downloadScatter.Add(testResult.date.ToOADate(), testResult.downSpeed);
            uploadScatter.Add(testResult.date.ToOADate(), testResult.upSpeed);

            if (testResult.downSpeed < minimumDownload) countBelowMinDownload++;
            if (testResult.upSpeed < minimumUpload) countBelowMinUpload++;

            if(render)
            {
                UpdatePlot();
            }
        }

        int numberOfMonthsEitherSideToDraw = 24;

        private void RedrawAverages()
        {
            uploadTrendlineScatter.Clear();
            downloadTrendlineScatter.Clear();

            var groupingIntervalInDays = Configuration.DaysForAverage();

            var groupedByInterval = testResults
                .GroupBy(result => result.date.Date.AddDays(-(result.date.Day % groupingIntervalInDays)))
                .Select(group => new BandwidthTest()
                {
                    downSpeed = group.Average(result => result.downSpeed),
                    upSpeed = group.Average(result => result.upSpeed),
                    date = DateTimeOffset.FromUnixTimeMilliseconds((long)(group.Average(result => (result.date - new DateTime(1970, 1, 1)).TotalMilliseconds))).DateTime
                })
                .OrderBy(group => group.date.ToOADate())
                .ToList();

            // Fill in periods of missing data to avoid trendlines gooing backwards
            List<BandwidthTest> filledData = new List<BandwidthTest>();

            int daysForAverage = Configuration.DaysForAverage();

            for (int i = 0; i < groupedByInterval.Count; i++)
            {
                filledData.Add(groupedByInterval[i]);

                if (i + 1 < groupedByInterval.Count)
                {
                    BandwidthTest currentPoint = groupedByInterval[i];
                    BandwidthTest nextPoint = groupedByInterval[i + 1];

                    double daysBetween = (nextPoint.date - currentPoint.date).TotalDays;

                    if (daysBetween > daysForAverage)
                    {
                        // Insert points along the line for days where there are no recordings
                        int numInsertions = (int)(daysBetween / daysForAverage) - 1;
                        TimeSpan timeStep = TimeSpan.FromDays(daysBetween / (numInsertions + 1));

                        for (int j = 1; j <= numInsertions; j++)
                        {
                            DateTime interpolatedDate = currentPoint.date + TimeSpan.FromDays(timeStep.TotalDays * j);

                            filledData.Add(new BandwidthTest
                            {
                                downSpeed = Lerp(currentPoint.downSpeed, nextPoint.downSpeed, j / (double)(numInsertions + 1)),
                                upSpeed = Lerp(currentPoint.upSpeed, nextPoint.upSpeed, j / (double)(numInsertions + 1)),
                                date = interpolatedDate
                            });
                        }
                    }
                }
            }

            foreach (var avg in filledData)
            {
                uploadTrendlineScatter.Add(avg.date.ToOADate(), avg.upSpeed);
                downloadTrendlineScatter.Add(avg.date.ToOADate(), avg.downSpeed);
            }
        }

        public static double Lerp(double start, double end, double ratio)
        {
            return start + (end - start) * ratio;
        }

        private DateTime AddMonth(DateTime date)
        {
            if (date.Month == 12)
            {
                return new DateTime(date.Year + 1, 1, date.Day, date.Hour, date.Minute, date.Second);
            }

            return new DateTime(date.Year, date.Month + 1, date.Day, date.Hour, date.Minute, date.Second);
        }

        private DateTime AddMonths(DateTime date, int months)
        {
            for (int i = 0; i < months; i++)
            {
                date = AddMonth(date);
            }

            return date;
        }

        private static void DrawMonthLines(Plot plot, List<BandwidthTest> testResults, int numberOfMonthsEitherSideToDraw)
        {
            ScottPlot.Alignment[] alignments = (ScottPlot.Alignment[])Enum.GetValues(typeof(ScottPlot.Alignment));
            string[] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            if (testResults.Count > 1)
            {
                var startMonthLines = testResults.First().date.AddMonths(-numberOfMonthsEitherSideToDraw);
                var endMonthLines = DateTime.Now.AddMonths(numberOfMonthsEitherSideToDraw);

                for (DateTime month = new DateTime(startMonthLines.Year, startMonthLines.Month, 1); month < endMonthLines; month = month.AddMonths(1))
                {
                    var monthLine = plot.AddVerticalLine(x: month.ToOADate(), color: Color.DarkSlateGray, width: 1, style: LineStyle.Solid);

                    string monthText = months[month.Month - 1];
                    if (month.Year != DateTime.Now.Year) monthText = month.Year.ToString() + Environment.NewLine + monthText;
                    var monthLabel = plot.AddText(monthText, month.ToOADate(), 0, size: 12, color: Color.DarkSlateGray);
                    monthLabel.Alignment = alignments[6];
                }
            }
        }

        private void AssignPlotStyle(Plot plot)
        {
            plot.YLabel("Speed (mbps)");
            plot.Style(ScottPlot.Style.Gray2);
            plot.Style(figureBackground: graphBackgroundColor);
            plot.XAxis.DateTimeFormat(true);
        }

        public void DrawViewer(PlotManager plotManager)
        {
            if (wpfPlot == null && viewer == null) return;

            var firstTime = this.firstTime;

            AxisLimits axisLimits = plot.GetAxisLimits();
            if(plotManager != null)
            {
                axisLimits = plotManager.plot.GetAxisLimits();
            }
            else
            {
                axisLimits = plot.GetAxisLimits();
            }
            var xMin = axisLimits.XMin;
            var xMax = axisLimits.XMax;

            plot.Clear();

            DrawMonthLines(plot, testResults, numberOfMonthsEitherSideToDraw);

            if (uploadTrendlineScatter != null) uploadTrendlineScatter.Clear();
            else uploadTrendlineScatter = plot.AddScatterList();
            uploadTrendlineScatter.Label = "Upload Trend";
            uploadTrendlineScatter.MarkerSize = 0;
            uploadTrendlineScatter.LineWidth = 8;
            uploadTrendlineScatter.Color = uploadTrendlineColor;
            uploadTrendlineScatter.Smooth = true;
            uploadTrendlineScatter.IsVisible = showUploadTrendline;

            if (downloadTrendlineScatter != null) downloadTrendlineScatter.Clear();
            else downloadTrendlineScatter = plot.AddScatterList();
            downloadTrendlineScatter.Label = "Download Trend";
            downloadTrendlineScatter.MarkerSize = 0;
            downloadTrendlineScatter.LineWidth = 8;
            downloadTrendlineScatter.Color = downloadTrendlineColor;
            downloadTrendlineScatter.Smooth = true;
            downloadTrendlineScatter.IsVisible = showDownloadTrendline;

            if (showDownloadCandles)
            {
                if (downloadUpperPolygons != null)
                {
                    downloadUpperPolygons.Polys.Clear();
                    plot.Remove(downloadUpperPolygons);
                }
                if (downloadLowerPolygons != null)
                {
                    downloadLowerPolygons.Polys.Clear();
                    plot.Remove(downloadLowerPolygons);
                }
                if (downloadUpperLines != null)
                    foreach (var line in downloadUpperLines)
                        plot.Remove(line);
                if (downloadLowerLines != null)
                    foreach (var line in downloadLowerLines)
                        plot.Remove(line);
                DrawCandles(true);
            }

            if (showUploadCandles)
            {
                if (uploadUpperPolygons != null)
                {
                    uploadUpperPolygons.Polys.Clear();
                    plot.Remove(uploadUpperPolygons);
                }
                if (uploadLowerPolygons != null)
                {
                    uploadLowerPolygons.Polys.Clear();
                    plot.Remove(uploadLowerPolygons);
                }
                if (uploadUpperLines != null)
                    foreach (var line in uploadUpperLines)
                        plot.Remove(line);
                if (uploadLowerLines != null)
                    foreach (var line in uploadLowerLines)
                        plot.Remove(line);
                DrawCandles(false);
            }

            if (uploadScatter != null) uploadScatter.Clear();
            else uploadScatter = plot.AddScatterList();
            uploadScatter.Label = "Upload";
            uploadScatter.MarkerSize = 6;
            uploadScatter.Color = uploadLineColor;
            uploadScatter.LineWidth = 2;
            uploadScatter.IsVisible = showUpload;

            if (downloadScatter != null) downloadScatter.Clear();
            else downloadScatter = plot.AddScatterList();
            downloadScatter.Label = "Download";
            downloadScatter.MarkerSize = 6;
            downloadScatter.Color = downloadLineColor;
            downloadScatter.LineWidth = 2;
            downloadScatter.IsVisible = showDownload;

            if (uploadHLineVector != null) plot.Remove(uploadHLineVector);
            else uploadHLineVector = plot.AddHorizontalLine(minimumUpload);
            uploadHLineVector.Label = "Min. Upload";
            uploadHLineVector.Color = minUploadColor;
            uploadHLineVector.LineStyle = LineStyle.Dash;
            uploadHLineVector.IsVisible = showMinUpload;

            if (downloadHLineVector != null) plot.Remove(downloadHLineVector);
            else downloadHLineVector = plot.AddHorizontalLine(minimumDownload);
            downloadHLineVector.Label = "Min. Download";
            downloadHLineVector.Color = minDownloadColor;
            downloadHLineVector.LineStyle = LineStyle.Dash;
            downloadHLineVector.IsVisible = showMinDownload;

            RedrawAverages();

            foreach (var testResult in testResults)
            {
                AddResult(testResult);
            }

            var highestYValue = testResults.Select(result => result.downSpeed > result.upSpeed ? result.downSpeed : result.upSpeed).Max();
            plot.SetAxisLimitsY(0, highestYValue + 10 - (highestYValue % 10));

            AssignPlotStyle(plot);

            if (!firstTime || plotManager != null)
            {
                plot.SetAxisLimitsX(xMin, xMax);
            }
            else
            {
                // Work out starting XAxis span
                bool moreThan31DaysOfResults = false;

                if (testResults.Select(result => result.date).Min() < DateTime.Now.AddDays(-31)) moreThan31DaysOfResults = true;

                if (testResults.Count == 0)
                    plot.SetAxisLimitsX(DateTime.Now.AddDays(-1).ToOADate(), DateTime.Now.AddDays(1).ToOADate());
                else if (moreThan31DaysOfResults || testResults.Count == 1)
                    plot.SetAxisLimitsX(DateTime.Now.AddDays(-34).ToOADate(), testResults[testResults.Count - 1].date.AddDays(3).ToOADate());
                else
                {
                    double lengthOfTests = (testResults[testResults.Count - 1].date - testResults[0].date).TotalMinutes;
                    if (lengthOfTests < TimeSpan.FromHours(2).TotalMinutes)
                    {
                        plot.SetAxisLimitsX(testResults[0].date.AddHours(-1).ToOADate(),
                            testResults[testResults.Count - 1].date.AddHours(1).ToOADate());
                    }
                    else
                    {
                        plot.SetAxisLimitsX(testResults[0].date.AddMinutes(-(lengthOfTests / 5)).ToOADate(),
                            testResults[testResults.Count - 1].date.AddMinutes(lengthOfTests / 5).ToOADate());
                    }
                }
            }
        }

        //-----------------------------------

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
    }
}
