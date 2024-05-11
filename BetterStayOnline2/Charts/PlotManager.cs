using BetterStayOnline2.Editing;
using BetterStayOnline2.MVVM.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Transactions;
using Color = ScottPlot.Color;
using Fonts = ScottPlot.Fonts;

namespace BetterStayOnline2.Charts
{
    public static class PlotManager
    {
        public static WpfPlot ResultsTable = null;

        static double[] downloadSpeeds = { };
        static double[] uploadSpeeds = { };
        static double[] datetimes = { };
        static string[] networkNames = { };
        static string[] isps = { };

        private static ConnectionOutage[] outages = new ConnectionOutage[0];

        static PlotManager()
        {
            AssignVividDarkTheme();
        }

        public static void FillData(ObservableCollection<Network> networkList, ObservableCollection<EditableResult> editableResults)
        {
            NetworkList = networkList;
            EditableResults = editableResults;

            AddTestResults(ReadPreexistingTestResults());

            outages = ReadPreexistingOutages().ToArray();
        }

        public static void AddDatapoint(double down, double up, double time, string network, string isp)
        {
            double[] newDownloadSpeeds = new double[downloadSpeeds.Length + 1];
            double[] newUploadSpeeds = new double[uploadSpeeds.Length + 1];
            double[] newDatetimes = new double[datetimes.Length + 1];
            string[] newNetworkNames = new string[networkNames.Length + 1];
            string[] newIsps = new string[isps.Length + 1];

            for (int i = 0; i < downloadSpeeds.Length; i++)
            {
                newDownloadSpeeds[i] = downloadSpeeds[i];
                newUploadSpeeds[i] = uploadSpeeds[i];
                newDatetimes[i] = datetimes[i];
                newNetworkNames[i] = networkNames[i];
                newIsps[i] = isps[i];
            }

            newDownloadSpeeds[downloadSpeeds.Length] = down;
            newUploadSpeeds[uploadSpeeds.Length] = up;
            newDatetimes[datetimes.Length] = time;
            newNetworkNames[networkNames.Length] = network;
            newIsps[isps.Length] = isp;

            downloadSpeeds = newDownloadSpeeds;
            uploadSpeeds = newUploadSpeeds;
            datetimes = newDatetimes;
            networkNames = newNetworkNames;
            isps = newIsps;

            AddNetworkToNetworkList(network, isp);
            AddEditableResult(down, up, time, network, isp);
        }

        public static void RemoveDatapoint(double down, double up, double time, string network, string isp)
        {
            if (downloadSpeeds.Length == 0)
            {
                return;
            }

            bool removeNetwork = true;

            int newSize = 0;
            for (int i = 0; i < downloadSpeeds.Length; i++)
            {
                if (downloadSpeeds[i] == down &&
                    uploadSpeeds[i] == up &&
                    datetimes[i] == time &&
                    networkNames[i] == network &&
                    isps[i] == isp)
                {
                    continue;
                }

                if(networkNames[i] == network && isps[i] == isp)
                {
                    removeNetwork = false;
                }

                downloadSpeeds[newSize] = downloadSpeeds[i];
                uploadSpeeds[newSize] = uploadSpeeds[i];
                datetimes[newSize] = datetimes[i];
                networkNames[newSize] = networkNames[i];
                isps[newSize] = isps[i];

                newSize++;
            }

            Array.Resize(ref downloadSpeeds, newSize);
            Array.Resize(ref uploadSpeeds, newSize);
            Array.Resize(ref datetimes, newSize);
            Array.Resize(ref networkNames, newSize);
            Array.Resize(ref isps, newSize);

            if (removeNetwork)
            {
                RemoveNetworkFromNetworkList(network, isp);
            }
            RemoveEditableResult(down, up, time, network, isp);
        }

        private static bool tableHasBeenRefreshedSinceCreation = true;

        // Update ResultsTable with new data
        public static void UpdatePlot()
        {
            // TODO: Get from settings
            bool drawDownloadScatter = Configuration.ShowDownloadPoints();
            bool drawUploadScatter = Configuration.ShowUploadPoints();
            bool drawOutages = Configuration.ShowOutages();
            bool drawDownloadTrendline = Configuration.ShowDownloadTrendline();
            bool drawUploadTrendline = Configuration.ShowUploadTrendline();
            int trendByDays = Configuration.DaysForAverage();
            bool drawDownloadCandles = Configuration.ShowDownloadCandles();
            bool drawUploadCandles = Configuration.ShowUploadCandles();
            string candleDays = Configuration.CandlePeriod();
            bool drawMinDownload = Configuration.ShowMinDown();
            bool drawMinUpload = Configuration.ShowMinUp();
            double minDownloadValue = Configuration.MinDown();
            double minUploadValue = Configuration.MinUp();
            bool drawPercentAboveMinimums = Configuration.ShowPercentagesAboveMinimums();

            ResultsTable.Plot.Clear();

            // Redraw monthlines as they've just been cleared
            int numberOfMonthsEitherSideToDraw = 24;
            DrawMonthLines(numberOfMonthsEitherSideToDraw);

            // Get indexes of values to show by looking at the network list
            var indexesToShow = GetIndexesOfResultsToShowFromNetworkList();

            double[] downloadSpeeds = GetDownloadSpeedsFromIndexesToShow(indexesToShow);
            double[] uploadSpeeds = GetUploadSpeedsFromIndexesToShow(indexesToShow);
            double[] datetimes = GetDatetimesFromIndexesToShow(indexesToShow);

            // Draw data
            // We draw upload before download usually as download should be in front of upload

            // Draw outage blocks behind everything else
            if (drawOutages)
            {
                DrawOutages();
            }

            // Draw upload trendline
            if (drawDownloadTrendline)
            {
                DrawTrendlines(downloadSpeeds, uploadSpeeds, datetimes,
                    trendByDays);
            }
            // Draw download trendline
            if (drawUploadTrendline)
            {
                DrawTrendlines(downloadSpeeds, uploadSpeeds, datetimes, 
                    trendByDays, false);
            }

            // Show Upload Candles on top of Download candles because upload usually has smaller vertical range and will cover less
            // Draw Download Candles
            if (drawDownloadCandles)
            {
                DrawCandles(datetimes, 
                    candleDays, downloadSpeeds);
            }
            // Draw Upload Candles
            if (drawUploadCandles)
            {
                DrawCandles(datetimes, 
                    candleDays, uploadSpeeds, false);
            }

            // Draw upload scatter
            if (drawUploadScatter)
            {
                var uploadScatter = ResultsTable.Plot.Add.Scatter(datetimes, uploadSpeeds, 
                    uploadLineColor);
                uploadScatter.MarkerStyle.Outline.Color = uploadLineColor;
            }
            // Draw download scatter
            if (drawDownloadScatter)
            {
                var downloadScatter = ResultsTable.Plot.Add.Scatter(datetimes, downloadSpeeds, 
                    downloadLineColor);
                downloadScatter.MarkerStyle.Outline.Color = downloadLineColor;
            }

            // Draw min download line
            if (drawMinDownload)
            {
                DrawMinLine(minDownloadValue);
            }
            // Draw min upload line
            if (drawMinUpload)
            {
                DrawMinLine(minUploadValue, false);
            }
            // Draw percentage above minimums
            if(drawPercentAboveMinimums && (drawMinDownload || drawMinUpload))
            {
                DrawAmountAboveAverageAxisLabel(downloadSpeeds, uploadSpeeds, datetimes, 
                    minDownloadValue, minUploadValue, drawMinDownload, drawMinUpload);
            }

            if (tableHasBeenRefreshedSinceCreation)
            {
                CopyXAxisView();
                SetYAxisView(downloadSpeeds);
            }
            else
            {
                SetXAxisStartingView(datetimes);
            }

            ResultsTable.Refresh();

            tableHasBeenRefreshedSinceCreation = true;
        }

        // Create ResultsTable
        // Should only be called in one place, on ResultsTable construction
        // Nowhere in here should we be drawing any plots, as they will be cleared immediately after
        public static void DrawTable()
        {
            var indexesToShow = GetIndexesOfResultsToShowFromNetworkList();
            double[] downloadSpeeds = GetDownloadSpeedsFromIndexesToShow(indexesToShow);
            double[] datetimes = GetDatetimesFromIndexesToShow(indexesToShow);

            SetXAxisStartingView(datetimes);

            SetYAxisView(downloadSpeeds);

            LockVerticalZoom();

            AssignXAxisDateFormat();

            AssignPlotStyle();

            tableHasBeenRefreshedSinceCreation = false;
        }

        #region Tasks

        public static event EventHandler TestStarted;
        public static event EventHandler TestCompleted;

        public static void RunSpeedtest()
        {
            TestStarted?.Invoke(null, EventArgs.Empty);

            Thread thread = null;

            thread = new Thread(new ThreadStart(() =>
            {
                BandwidthTest? test = Speedtester.RunSpeedTest();
                if (test != null)
                {
                    BandwidthTest Test = (BandwidthTest)test;
                    AddDatapoint(Test.downSpeed, Test.upSpeed, Test.date.ToOADate(), Test.networkName, Test.isp);

                    if(CurrentContext.currentView == "HomeViewModel")
                        UpdatePlot();
                }

                TestCompleted?.Invoke(null, EventArgs.Empty);
            }));
            thread.Start();
        }

        #endregion

        #region PlotResultsTables

        // Note: For all draw functions we must pass in arrays of data as we filter the original arrays depending on NetworkList.Show

        // Month lines are created using Vertical Lines which are plots
        private static void DrawMonthLines(int numberOfMonthsEitherSideToDraw)
        {
            string[] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            if (datetimes.Length > 1)
            {
                var firstDate = DateTime.FromOADate(datetimes[0]).AddMonths(-numberOfMonthsEitherSideToDraw);

                var startMonthLines = DateTime.FromOADate(datetimes[0]).AddMonths(-numberOfMonthsEitherSideToDraw);
                var endMonthLines = DateTime.Now.AddMonths(numberOfMonthsEitherSideToDraw);

                for (DateTime month = new DateTime(startMonthLines.Year, startMonthLines.Month, 1); month < endMonthLines; month = month.AddMonths(1))
                {
                    var monthLine = ResultsTable.Plot.Add.VerticalLine(x: month.ToOADate(), color: monthLineColor, width: 2);

                    string monthText = months[month.Month - 1].Substring(0, 3);
                    if (month.Year != DateTime.Now.Year) monthText = monthText + " " + month.Year.ToString().Substring(2);

                    monthLine.Text = monthText;
                    monthLine.Label.LineSpacing = 2;
                    monthLine.LabelOppositeAxis = true;
                    monthLine.Label.BackColor = transparent;
                    monthLine.Label.Bold = false;
                    monthLine.Label.ForeColor = axisColor;
                }
            }
        }

        private static void DrawTrendlines(double[] downloadSpeeds, double[] uploadSpeeds, double[] datetimes,
            int trendByDays, bool download = true)
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

            var averageScatter = ResultsTable.Plot.Add.Scatter(dates.ToArray(), averages.ToArray(), download ? downloadTrendlineColor : uploadTrendlineColor);
            averageScatter.Label = (download ? "Download" : "Upload") + " Trend";
            averageScatter.LineWidth = 8;
            averageScatter.Smooth = true;
            averageScatter.MarkerStyle = MarkerStyle.None;
        }

        private static List<int> GetIndexesInDateRange(double[] datetimes,
            DateTime startDate, DateTime endDate)
        {
            List<int> indexesInRange = new List<int>();
            bool foundOne = false;
            for (int i = 0; i < datetimes.Length; i++)
            {
                DateTime date = DateTime.FromOADate(datetimes[i]);
                if (date > startDate && date <= endDate)
                {
                    indexesInRange.Add(i);
                    foundOne = true;
                }
                else
                {
                    if (foundOne) break;
                }
            }

            return indexesInRange;
        }

        private static void DrawAmountAboveAverageAxisLabel(double[] downloadSpeeds, double[] uploadSpeeds, double[] datetimes,
            double minDown, double minUp, bool showDown, bool showUp)
        {
            if (datetimes.Length > 1)
            {
                var startMonthLines = DateTime.FromOADate(datetimes[0]);
                var endMonthLines = DateTime.Now;

                List<DateTime> months = new List<DateTime>();

                for (DateTime month = new DateTime(startMonthLines.Year, startMonthLines.Month, 1); month < endMonthLines; month = AddMonth(month))
                {
                    DateTime nextMonth = AddMonth(month);

                    DateTime middleOfMonth = month.AddDays(((nextMonth - month).TotalDays / 2));

                    months.Add(middleOfMonth);
                }

                foreach (var month in months)
                {
                    DateTime startDate = new DateTime(month.Year, month.Month, 1);
                    DateTime nextMonth = AddMonth(startDate);

                    // Create an invisible line in the middle of the month with a label at the top axis
                    var invisibleDownloadLine = ResultsTable.Plot.Add.VerticalLine(x: month.ToOADate(), width: 0);
                    var invisibleUploadLine = ResultsTable.Plot.Add.VerticalLine(x: month.ToOADate(), width: 0);

                    List<int> indexesInRange = GetIndexesInDateRange(datetimes,
                        startDate, nextMonth);

                    if (indexesInRange.Count == 0) continue;

                    string label = "";

                    if (showDown)
                    {
                        double[] downloadSpeedsInRange = downloadSpeeds.Skip(indexesInRange.Min()).Take(indexesInRange.Count).ToArray();

                        int countAboveMinimum = downloadSpeedsInRange.Where(speed => speed > (minDown)).Count();

                        label = "Down: ";
                        if (countAboveMinimum > 0)
                        {
                            double perc = (double)countAboveMinimum / downloadSpeedsInRange.Length * 100;
                            label += perc.ToString("0.00") + "%";
                        }
                        else
                            label += "0%";

                        invisibleDownloadLine.Text = label;
                        invisibleDownloadLine.Label.LineSpacing = 2;
                        invisibleDownloadLine.LabelOppositeAxis = true;
                        invisibleDownloadLine.Label.BackColor = transparent;
                        invisibleDownloadLine.Label.Bold = false;
                        invisibleDownloadLine.Label.FontSize = 12;
                        invisibleDownloadLine.Label.ForeColor = axisColor;
                        invisibleDownloadLine.Label.Alignment = Alignment.UpperCenter;

                        if (showDown && showUp)
                        {
                            invisibleDownloadLine.Label.OffsetY = -4;
                        }
                    }

                    if (showUp)
                    {
                        double[] uploadSpeedsInRange = uploadSpeeds.Skip(indexesInRange.Min()).Take(indexesInRange.Count).ToArray();

                        int countAboveMinimum = uploadSpeedsInRange.Where(speed => speed > (minUp)).Count();

                        label = "Up: ";
                        if (countAboveMinimum > 0)
                        {
                            double perc = (double)countAboveMinimum / uploadSpeedsInRange.Length * 100;
                            label += perc.ToString("0.00") + "%";
                        }
                        else
                            label += "0%";

                        invisibleUploadLine.Text = label;
                        invisibleUploadLine.Label.LineSpacing = 2;
                        invisibleUploadLine.LabelOppositeAxis = true;
                        invisibleUploadLine.Label.BackColor = transparent;
                        invisibleUploadLine.Label.Bold = false;
                        invisibleUploadLine.Label.FontSize = 12;
                        invisibleUploadLine.Label.ForeColor = axisColor;
                        invisibleUploadLine.Label.Alignment = Alignment.UpperCenter;

                        if (showDown && showUp)
                        {
                            invisibleUploadLine.Label.OffsetY = 12;
                        }
                    }
                }
            }
        }

        private static void DrawCandles(double[] datetimes,
            string candlePeriod, double[] speeds, bool download = true)
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

                TimeSpan periodTimeSpan = TimeSpan.FromSeconds(0);
                try
                {
                    periodTimeSpan = endOfPeriodDate - currentDate;
                }
                catch(OverflowException oe)
                {
                    periodTimeSpan = TimeSpan.FromDays(29);
                }

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

                    LinePlot upLine = ResultsTable.Plot.Add.Line(new Coordinates(midPoint.ToOADate(), mean + leftStdDev), new Coordinates(midPoint.ToOADate(), speedsInRange.Max()));
                    upLine.Color = download ? downloadUpLineColor : uploadUpLineColor;

                    LinePlot lowerLine = ResultsTable.Plot.Add.Line(new Coordinates(midPoint.ToOADate(), mean - rightStdDev), new Coordinates(midPoint.ToOADate(), speedsInRange.Min()));
                    lowerLine.Color = download ? downloadDownLineColor : uploadDownLineColor;

                    upLine.LineWidth = candleLineWidth;
                    lowerLine.LineWidth = candleLineWidth;
                }
                else if (speedsInRange.Length == 2)
                {
                    var mean = CalculateMean(speedsInRange.ToArray());

                    LinePlot topVertLine = ResultsTable.Plot.Add.Line(new Coordinates(midPoint.ToOADate(), speedsInRange.Max()), new Coordinates(midPoint.ToOADate(), mean));
                    topVertLine.Color = download ? downloadUpLineColor : uploadUpLineColor;

                    LinePlot midLine = ResultsTable.Plot.Add.Line(new Coordinates(startPoint.ToOADate(), mean), new Coordinates(endPoint.ToOADate(), mean));
                    midLine.Color = download ? downloadDownLineColor : uploadDownLineColor;

                    LinePlot bottomVertLine = ResultsTable.Plot.Add.Line(new Coordinates(midPoint.ToOADate(), mean), new Coordinates(midPoint.ToOADate(), speedsInRange.Min()));
                    bottomVertLine.Color = download ? downloadDownLineColor : uploadDownLineColor;

                    topVertLine.LineWidth = candleLineWidth;
                    midLine.LineWidth = candleLineWidth;
                    bottomVertLine.LineWidth = candleLineWidth;
                }
                else if (speedsInRange.Length == 1)
                {
                    LinePlot line = ResultsTable.Plot.Add.Line(new Coordinates(startPoint.ToOADate(), speedsInRange[0]), new Coordinates(endPoint.ToOADate(), speedsInRange[0]));
                    line.Color = download ? downloadDownLineColor : uploadDownLineColor;

                    line.LineWidth = candleLineWidth;
                }
            }

            foreach (var polygon in upperBoxPolygons)
            {
                Polygon poly = ResultsTable.Plot.Add.Polygon(polygon);
                poly.FillStyle.Color = download ? downloadUpCandleColor : uploadUpCandleColor;
                poly.LineStyle.Color = download ? downloadUpLineColor : uploadUpLineColor;
                poly.LineStyle.Width = 0;
                poly.MarkerStyle.Outline.Color = transparent;
            }
            foreach (var polygon in lowerBoxPolygons)
            {
                Polygon poly = ResultsTable.Plot.Add.Polygon(polygon);
                poly.FillStyle.Color = download ? downloadDownCandleColor : uploadDownCandleColor;
                poly.LineStyle.Color = download ? downloadDownLineColor : uploadDownLineColor;
                poly.LineStyle.Width = 0;
                poly.MarkerStyle.Outline.Color = transparent;
            }
        }

        private static void DrawMinLine(double yVal, bool download = true)
        {
            var line = ResultsTable.Plot.Add.HorizontalLine(yVal);
            line.LineStyle.Color = download ? minDownloadColor : minUploadColor;
            line.LinePattern = LinePattern.Dashed;
            line.LineStyle.Width = 2;
        }

        private static void DrawOutages()
        {
            foreach(var outage in outages)
            {
                ResultsTable.Plot.Add.HorizontalSpan(outage.startTime, outage.endTime, new Color(149, 50, 217));
            }
        }

        #endregion

        #region Data Handling

        public struct BandwidthTest
        {
            public DateTime date;
            public double downSpeed;
            public double upSpeed;
            public string networkName;
            public string isp;
        }

        private static List<BandwidthTest> ReadPreexistingTestResults()
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
                        newTest.networkName = (string)((JObject)result).GetValue("NetworkName");
                        newTest.isp = (string)((JObject)result).GetValue("ISP");
                        testResults.Add(newTest);
                    }
                }
            }
            catch (Exception) { }

            return testResults;
        }

        private static List<ConnectionOutage> ReadPreexistingOutages()
        {
            JArray jsonOutages = new JArray();
            List<ConnectionOutage> outages = new List<ConnectionOutage>();

            try
            {
                string data = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\External\\testresults.json");
                JObject obj = JObject.Parse(data);
                if (obj.ContainsKey("Outages"))
                {
                    jsonOutages = (JArray)obj.GetValue("Outages");

                    outages.Clear();
                    foreach (var jsonOutage in jsonOutages)
                    {
                        ConnectionOutage outage = new ConnectionOutage();
                        outage.startTime = DateTime.Parse((string)((JObject)jsonOutage).GetValue("StartTime")).ToOADate();
                        outage.endTime = DateTime.Parse((string)((JObject)jsonOutage).GetValue("EndTime")).ToOADate();
                        outages.Add(outage);
                    }
                }
            }
            catch (Exception) { }

            return outages;
        }

        private static void AddTestResults(List<BandwidthTest> testResults)
        {
            foreach (var result in testResults)
            {
                AddTestResult(result);
            }
        }

        private static void AddTestResult(BandwidthTest result)
        {
            AddDatapoint(result.downSpeed, result.upSpeed, result.date.ToOADate(), result.networkName, result.isp);
        }

        #endregion

        #region Networks

        // Reference to SettingsViewModel.NetworkList
        public static ObservableCollection<Network> NetworkList { get; set; }

        private static void AddNetworkToNetworkList(string network, string isp)
        {
            // If this is a new network add it to the list
            if (!NetworkList.Any(n => n.Name == network && n.ISP == isp))
            {
                // Force NetworkList changing to the UI thread where it was created to avoid thread affinity issues
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    NetworkList.Add(new Network()
                    {
                        Name = network,
                        ISP = isp,
                        Show = true
                    });

                    NetworkList.OrderBy(n => n.ISP).ThenBy(n => n.Name);
                });
            }
        }

        private static void RemoveNetworkFromNetworkList(string network, string isp)
        {
            if (NetworkList.Any(n => n.Name == network && n.ISP == isp))
            {
                var networksToRemove = NetworkList.Where(n => n.Name == network && n.ISP == isp);

                // Force NetworkList changing to the UI thread where it was created to avoid thread affinity issues
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    foreach (var networkToRemove in networksToRemove)
                    {
                        NetworkList.Remove(networkToRemove);
                    }

                    NetworkList.OrderBy(n => n.ISP).ThenBy(n => n.Name);
                });
            }
        }

        private static int[] GetIndexesOfResultsToShowFromNetworkList()
        {
            Network[] networksToShow = NetworkList.Where(network => network.Show).ToArray();

            List<int> indexesToShow = new List<int>();

            for(int i = 0; i < networkNames.Length; i++)
            {
                if(networksToShow.Any(network => network.Name == networkNames[i] && network.ISP == isps[i]))
                {
                    indexesToShow.Add(i);
                }
            }

            return indexesToShow.ToArray();
        }

        private static double[] GetDownloadSpeedsFromIndexesToShow(int[] indexesToShow)
        {
            double[] selectedItems = new double[indexesToShow.Length];
            for (int i = 0; i < indexesToShow.Length; i++)
            {
                int index = indexesToShow[i];
                selectedItems[i] = downloadSpeeds[index];
            }

            return selectedItems;
        }

        private static double[] GetUploadSpeedsFromIndexesToShow(int[] indexesToShow)
        {
            double[] selectedItems = new double[indexesToShow.Length];
            for (int i = 0; i < indexesToShow.Length; i++)
            {
                int index = indexesToShow[i];
                selectedItems[i] = uploadSpeeds[index];
            }

            return selectedItems;
        }

        private static double[] GetDatetimesFromIndexesToShow(int[] indexesToShow)
        {
            double[] selectedItems = new double[indexesToShow.Length];
            for (int i = 0; i < indexesToShow.Length; i++)
            {
                int index = indexesToShow[i];
                selectedItems[i] = datetimes[index];
            }

            return selectedItems;
        }

        #endregion

        #region Editable Results

        public static ObservableCollection<EditableResult> EditableResults;

        private static void AddEditableResult(double down, double up, double time, string network, string isp)
        {
            // Force EditableResults changing to the UI thread where it was created to avoid thread affinity issues
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                EditableResults.Add(new EditableResult()
                {
                    DownSpeed = down,
                    UpSpeed = up,
                    Datetime = DateTime.FromOADate(time),
                    NetworkName = network,
                    ISP = isp,
                    Selected = false
                });

                EditableResults.OrderBy(n => n.Datetime);
            });
        }

        private static void RemoveEditableResult(double down, double up, double time, string network, string isp)
        {
            var editableResultsToRemove = EditableResults.Where(er =>
            {
                return er.Datetime.ToOADate() == time &&
                    er.DownSpeed == down &&
                    er.UpSpeed == up &&
                    er.NetworkName == network &&
                    er.ISP == isp;
            }).ToList();

            // Force EditableResults changing to the UI thread where it was created to avoid thread affinity issues
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                foreach(var editableResult in editableResultsToRemove)
                {
                    EditableResults.Remove(editableResult);
                }

                EditableResults.OrderBy(n => n.Datetime);
            });
        }

        #endregion

        #region Table Drawing

        private static void SetXAxisStartingView(double[] datetimes)
        {
            bool moreThan31DaysOfResults = false;

            if (datetimes.Length > 0 && DateTime.FromOADate(datetimes.Min()) < DateTime.FromOADate(datetimes.Max()).AddDays(-31)) moreThan31DaysOfResults = true;

            if (datetimes.Length == 0)
                ResultsTable.Plot.Axes.SetLimitsX(DateTime.Now.AddDays(-1).ToOADate(), DateTime.Now.AddDays(1).ToOADate());
            else if (moreThan31DaysOfResults || datetimes.Length == 1)
                ResultsTable.Plot.Axes.SetLimitsX(DateTime.Now.AddDays(-34).ToOADate(), DateTime.FromOADate(datetimes.Max()).AddDays(3).ToOADate());
            else
            {
                double lengthOfTests = (DateTime.FromOADate(datetimes.Max()) - DateTime.FromOADate(datetimes.Min())).TotalHours;
                double hours = lengthOfTests / 60;

                if(hours < 22) // Less than a day, show a day
                {
                    double minutesEitherSide = (24 - lengthOfTests) / 2;

                    ResultsTable.Plot.Axes.SetLimitsX(DateTime.FromOADate(datetimes.Min()).AddHours(-minutesEitherSide).ToOADate(), 
                        DateTime.FromOADate(datetimes.Max()).AddHours(minutesEitherSide).ToOADate());
                }
                else if (hours < 3 * 22) // Less than 3 days, show 3 hours either side
                {
                    double minutesEitherSide = 60 * 3;

                    ResultsTable.Plot.Axes.SetLimitsX(DateTime.FromOADate(datetimes.Min()).AddHours(-minutesEitherSide).ToOADate(),
                        DateTime.FromOADate(datetimes.Max()).AddHours(minutesEitherSide).ToOADate());
                }
                else if (hours < 3 * 22) // Less than a week, show 1 day either side
                {
                    double minutesEitherSide = 60 * 24;

                    ResultsTable.Plot.Axes.SetLimitsX(DateTime.FromOADate(datetimes.Min()).AddHours(-minutesEitherSide).ToOADate(),
                        DateTime.FromOADate(datetimes.Max()).AddHours(minutesEitherSide).ToOADate());
                }
                else // Show 3 days  either side
                {
                    double minutesEitherSide = 60 * 24 * 3;

                    ResultsTable.Plot.Axes.SetLimitsX(DateTime.FromOADate(datetimes.Min()).AddHours(-minutesEitherSide).ToOADate(),
                        DateTime.FromOADate(datetimes.Max()).AddHours(minutesEitherSide).ToOADate());
                }
            }
        }

        private static void CopyXAxisView()
        {
            AxisLimits axisLimits = ResultsTable.Plot.Axes.GetLimits();

            ResultsTable.Plot.Axes.SetLimitsX(axisLimits.Left, axisLimits.Right);
        }

        private static void SetYAxisView(double[] downloadSpeeds)
        {
            double yAxisTopLimit = currentTopYAxis;

            if (downloadSpeeds.Length > 0)
            {
                var highestYValue = downloadSpeeds.Concat(uploadSpeeds).Max();
                yAxisTopLimit = highestYValue + 10 - (highestYValue % 10);

                if(yAxisTopLimit != currentTopYAxis)
                {
                    currentTopYAxis = yAxisTopLimit;
                }
            }
            ResultsTable.Plot.Axes.SetLimitsY(0, yAxisTopLimit);

            CreateYAxisTicks(yAxisTopLimit);
        }

        private static void CreateYAxisTicks(double yAxisTopLimit)
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

            ResultsTable.Plot.Axes.Left.TickGenerator = ticks;
        }

        // Fixed LockedVertical Axis Rule because ScottPlott wasn't working probably
        class NewLockedVertical : IAxisRule
        {
            private readonly IYAxis YAxis;

            public NewLockedVertical(IYAxis yAxis)
            {
                YAxis = yAxis;
            }

            public void Apply(RenderPack rp, bool beforeLayout)
            {
                if (rp.Plot.LastRender.Count != 0)
                {
                    double bottom = 0;
                    double top = PlotManager.currentTopYAxis;
                    YAxis.Range.Set(bottom, top);
                }
            }
        }

        public static double currentTopYAxis { get; private set; } = 100;

        private static void LockVerticalZoom()
        {
            NewLockedVertical lockedVerticalRule = new NewLockedVertical(ResultsTable.Plot.Axes.Left);

            ResultsTable.Plot.Axes.Rules.Clear();
            ResultsTable.Plot.Axes.Rules.Add(lockedVerticalRule);
        }

        private static void AssignXAxisDateFormat()
        {
            ResultsTable.Plot.Axes.DateTimeTicksBottom();
        }

        #endregion

        #region Connection Losses

        private struct ConnectionOutage
        {
            public double startTime;
            public double endTime;
        }

        private static double invalidOutageStartTimeConst { get { return -99999; } }
        private static double outageStarted = invalidOutageStartTimeConst;

        public static void NetworkConnectionLost()
        {
            outageStarted = DateTime.Now.ToOADate();
        }

        public static void NetworkConnectionEstablished()
        {
            if (outageStarted == invalidOutageStartTimeConst) return;

            ConnectionOutage outage = new ConnectionOutage()
            {
                startTime = outageStarted,
                endTime = DateTime.Now.ToOADate()
            };

            ConnectionOutage[] newArray = new ConnectionOutage[outages.Length + 1];
            Array.Copy(outages, newArray, outages.Length);
            newArray[newArray.Length - 1] = outage;

            outages = newArray;

            UpdatePlot();

            outageStarted = invalidOutageStartTimeConst;

#if !DEBUG
            // Save outage to json
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\External\\testresults.json";

            try
            {
                using (var scope = new TransactionScope())
                {
                    try
                    {
                        // Read existing data
                        string existingData = File.ReadAllText(path);
                        JObject obj = JObject.Parse(existingData);

                        JArray outagesArr = new JArray();

                        foreach(var o in outages)
                        {
                            JObject jsonOutage = new JObject();

                            jsonOutage["StartTime"] = o.startTime;
                            jsonOutage["EndTime"] = o.endTime;

                            outagesArr.Add(jsonOutage);
                        }

                        obj["Outages"] = outagesArr;

                        // Validate updated JSON
                        try
                        {
                            JToken.Parse(obj.ToString());
                        }
                        catch (JsonReaderException)
                        {
                            Console.WriteLine("Updated JSON is not valid. Aborting update.");
                            return;
                        }
                        finally
                        {
                            // Perform atomic write to the file
                            string tempPath = path + ".temp";
                            File.WriteAllText(tempPath, obj.ToString());
                            File.Replace(tempPath, path, null);

                            scope.Complete(); // Commit the transaction
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions or log errors
                        Console.WriteLine($"Error processing outage: {ex.Message}");
                    }
                }
            }
            catch (TransactionAbortedException)
            {
                // Transaction was rolled back
                Console.WriteLine("Transaction aborted. File not modified.");
            }
#endif
        }

        #endregion

        #region Themes

        #region Vivid Dark Theme

        private static Color transparent = new Color(0, 0, 0, 0);
        private static Color charcoal = new Color(31, 31, 31);
        private static Color darkCharcoal = new Color(25, 24, 24);
        private static Color grey = new Color(153, 170, 181);
        private static Color darkGrey = new Color(38, 39, 41);
        private static Color lightGrey = new Color(51, 50, 50);
        private static Color electricBlue = new Color(100, 149, 237);
        private static Color fireRed = new Color(255, 69, 0);
        private static Color deepSkyBlue = new Color(0, 191, 255);
        private static Color orange = new Color(255, 165, 0);
        private static Color darkSlateBlue = new Color(72, 61, 139);
        private static Color brown = new Color(165, 42, 42);
        private static Color vividAgave = new Color(18, 153, 113);
        private static Color darkWindowsBlue = new Color(29, 99, 168);
        private static Color mustard = new Color(243, 182, 5);
        private static Color darkPastelRed = new Color(200, 72, 72);
        private static Color violet = new Color(85, 38, 117);

        private static void AssignVividDarkTheme()
        {
            appColor = charcoal;

            outerGraphColor = transparent;
            graphBackgroundColor = darkCharcoal;
            axisColor = grey;
            majorLineColor = darkGrey;
            monthLineColor = lightGrey;

            downloadLineColor = electricBlue;
            uploadLineColor = fireRed;

            minDownloadColor = deepSkyBlue;
            minUploadColor = orange;

            downloadTrendlineColor = darkSlateBlue;
            uploadTrendlineColor = brown;

            downloadUpCandleColor = vividAgave;
            downloadUpLineColor = downloadUpCandleColor;

            downloadDownCandleColor = darkWindowsBlue;
            downloadDownLineColor = downloadDownCandleColor;

            uploadUpCandleColor = mustard;
            uploadUpLineColor = uploadUpCandleColor;

            uploadDownCandleColor = darkPastelRed;
            uploadDownLineColor = uploadDownCandleColor;

            outageBlockColor = violet;
        }

        #endregion

        #endregion

        #region Styles

        public static Color appColor;

        private static Color outerGraphColor;
        private static Color graphBackgroundColor;
        private static Color axisColor;
        private static Color majorLineColor;
        private static Color monthLineColor;

        private static Color downloadLineColor;
        private static Color uploadLineColor;

        private static Color minDownloadColor;
        private static Color minUploadColor;

        private static Color downloadTrendlineColor;
        private static Color uploadTrendlineColor;

        private static Color downloadUpCandleColor;
        private static Color downloadUpLineColor;

        private static Color downloadDownCandleColor;
        private static Color downloadDownLineColor;

        private static Color uploadUpCandleColor;
        private static Color uploadUpLineColor;

        private static Color uploadDownCandleColor;
        private static Color uploadDownLineColor;

        private static Color outageBlockColor;
        
        private static int candleLineWidth = 2;

        private static void AssignPlotStyle()
        {
            ResultsTable.Plot.FigureBackground.Color = outerGraphColor;
            ResultsTable.Plot.DataBackground.Color = graphBackgroundColor;
            ResultsTable.Plot.Axes.Color(axisColor);
            ResultsTable.Plot.Grid.MajorLineColor = majorLineColor;

            Fonts.Default = "Aptos";

            ResultsTable.Plot.Axes.Bottom.TickLabelStyle.FontName = Fonts.Default;

            ResultsTable.Plot.Font.Set(Fonts.Default);
            ResultsTable.Plot.Axes.Bottom.TickLabelStyle.FontSize = 13;

            ResultsTable.Plot.Axes.FrameColor(transparent);
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