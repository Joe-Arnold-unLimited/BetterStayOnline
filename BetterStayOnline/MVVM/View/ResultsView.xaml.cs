using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using BetterStayOnline.SpeedTest;
using System.IO;
using Newtonsoft.Json.Linq;
using BetterStayOnline.MVVM.Model;
using ScottPlot.Plottable;
using Microsoft.Win32;
using System.Drawing;
using Microsoft.Office.Interop.Excel;
using ScottPlot;
using ScottPlot.Drawing;
using MenuItem = System.Windows.Controls.MenuItem;
using System.Reflection;
using SixLabors.ImageSharp.Processing.Processors.Convolution;

namespace BetterStayOnline.MVVM.View
{
    /// <summary>
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        private JArray jsonTestResults = new JArray();
        public List<BandwidthTest> testResults = new List<BandwidthTest>();

        Plot resultsTableCopy = null;
        WpfPlotViewer viewer = null;

        private ScatterPlotList<double> downloadScatter;
        private ScatterPlotList<double> uploadScatter;

        private ScatterPlotList<double> downloadAverageScatter;
        private ScatterPlotList<double> uploadAverageScatter;

        // Keep a local copy of speeds for spans because we can't keep testResults live
        private List<double> downloadSpeeds = new List<double>();
        private VSpan vSpanDownload;
        private List<double> uploadSpeeds = new List<double>();
        private VSpan vSpanUpload;

        private int minimumDownload;
        private int minimumUpload;
        private int countBelowMinDownload;
        private int countBelowMinUpload;

        double highestYValue = 0;

        List<Timer> eventTimers;
        Func<ResultsView, bool> redraw = (r) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                r.ReadPreexistingData();
                r.testResults = r.testResults.OrderBy(x => x.date).ToList();

                r.downloadScatter.Clear();
                r.uploadScatter.Clear();

                r.highestYValue = 0;
                foreach (var testResult in r.testResults)
                {
                    r.AddResult(testResult);
                    if (testResult.downSpeed > r.highestYValue) r.highestYValue = testResult.downSpeed;
                    if (testResult.upSpeed > r.highestYValue) r.highestYValue = testResult.upSpeed;
                }

                r.RedrawAverages();

                r.SetYAxisLimits(r.ResultsTable.Plot, r.highestYValue);
                r.AddVerticalSpan();

                r.CalculatePercentageBelowMinimums();
                r.ResultsTable.Render();
            });
            return true;
        };

        // TODO: Move to settings
        Color uploadAverageColor;
        Color uploadLineColor;
        Color minUploadColor;
        Color downloadAverageColor;
        Color downloadLineColor;
        Color minDownloadColor;
        Color graphBackgroundColor;

        public ResultsView()
        {
            InitializeComponent();

            uploadAverageColor = Color.Brown;
            uploadLineColor = Color.OrangeRed;
            minUploadColor = Color.Orange;
            downloadAverageColor = Color.DarkSlateBlue;
            downloadLineColor = Color.CornflowerBlue;
            minDownloadColor = Color.DeepSkyBlue;
            graphBackgroundColor = Color.FromArgb(7, 38, 59);

            ResultsTable.RightClicked -= ResultsTable.DefaultRightClickEvent;
            ResultsTable.RightClicked += DeployMainWindowMenu;

            ReadPreexistingData();
            ResultsTable.Plot.YLabel("Speed (mbps)");
            ResultsTable.Plot.Style(ScottPlot.Style.Gray2);
            ResultsTable.Plot.Style(figureBackground: graphBackgroundColor);
            ResultsTable.Plot.XAxis.DateTimeFormat(true);
            ResultsTable.Plot.SetAxisLimitsY(0, 100);
            ResultsTable.Configuration.LockVerticalAxis = true;

            minimumDownload = Configuration.MinDown();
            minimumUpload = Configuration.MinUp();
            countBelowMinDownload = 0;
            countBelowMinUpload = 0;

            if (Configuration.ShowPercentagesBelowMinimums() && (Configuration.ShowMinDown() || Configuration.ShowMinUp()))
            {
                PercentagesBelowMinimumsBlock.Visibility = Visibility.Visible;
                if (Configuration.ShowMinDown())
                {
                    DownloadMinPercentage.Visibility = Visibility.Visible;
                    DownloadMinPercentageValue.Visibility = Visibility.Visible;
                }
                else
                {
                    DownloadMinPercentage.Visibility = Visibility.Collapsed;
                    DownloadMinPercentageValue.Visibility = Visibility.Collapsed;
                }
                if (Configuration.ShowMinUp())
                {
                    UploadMinPercentage.Visibility = Visibility.Visible;
                    UploadMinPercentageValue.Visibility = Visibility.Visible;
                }
                else
                {
                    UploadMinPercentage.Visibility = Visibility.Collapsed;
                    UploadMinPercentageValue.Visibility = Visibility.Collapsed;
                }
            }
            else PercentagesBelowMinimumsBlock.Visibility = Visibility.Collapsed;

            AddVerticalSpan();
            DrawMonthLines();
            SetUpTable();
            eventTimers = new List<Timer>();
            eventTimers = TimerFactory.CreateTimers(eventTimers, EventReader.GetEvents(), redraw, this).ToList();
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

        private void SetUpTable()
        {
            ResultsTable.Plot.Clear();
            testResults = testResults.OrderBy(x => x.date).ToList();

            uploadAverageScatter = ResultsTable.Plot.AddScatterList();
            uploadAverageScatter.Label = "Upload Avg";
            uploadAverageScatter.MarkerSize = 0;
            uploadAverageScatter.LineWidth = 8;
            uploadAverageScatter.Color = uploadAverageColor;
            uploadAverageScatter.Smooth = true;
            uploadAverageScatter.IsVisible = Configuration.ShowUploadTrendline();

            downloadAverageScatter = ResultsTable.Plot.AddScatterList();
            downloadAverageScatter.Label = "Download Avg";
            downloadAverageScatter.MarkerSize = 0;
            downloadAverageScatter.LineWidth = 8;
            downloadAverageScatter.Color = downloadAverageColor;
            downloadAverageScatter.Smooth = true;
            downloadAverageScatter.IsVisible = Configuration.ShowDownloadTrendline();

            uploadScatter = ResultsTable.Plot.AddScatterList();
            uploadScatter.Label = "Upload";
            uploadScatter.MarkerSize = 6;
            uploadScatter.Color = uploadLineColor;
            uploadScatter.LineWidth = 2;
            uploadScatter.IsVisible = Configuration.ShowUploadPoints();

            downloadScatter = ResultsTable.Plot.AddScatterList();
            downloadScatter.Label = "Download";
            downloadScatter.MarkerSize = 6;
            downloadScatter.Color = downloadLineColor;
            downloadScatter.LineWidth = 2;
            downloadScatter.IsVisible = Configuration.ShowDownloadPoints();

            bool moreThan31DaysOfResults = false;
            highestYValue = testResults.Select(result => result.downSpeed > result.upSpeed ? result.downSpeed : result.upSpeed).Max();
            foreach (var testResult in testResults)
            {
                AddResult(testResult);
                if (!moreThan31DaysOfResults && testResult.date < DateTime.Now.AddDays(-31)) moreThan31DaysOfResults = true;
            }

            SetYAxisLimits(ResultsTable.Plot, highestYValue);

            if (testResults.Count == 0)
                ResultsTable.Plot.SetAxisLimitsX(DateTime.Now.AddDays(-1).ToOADate(), DateTime.Now.AddDays(1).ToOADate());
            else if(moreThan31DaysOfResults || testResults.Count == 1)
                ResultsTable.Plot.SetAxisLimitsX(DateTime.Now.AddDays(-34).ToOADate(), testResults[testResults.Count - 1].date.AddDays(3).ToOADate());
            else
            {
                double lengthOfTests = (testResults[testResults.Count - 1].date - testResults[0].date).TotalMinutes;
                if (lengthOfTests < TimeSpan.FromHours(2).TotalMinutes)
                {
                    ResultsTable.Plot.SetAxisLimitsX(testResults[0].date.AddHours(-1).ToOADate(),
                        testResults[testResults.Count - 1].date.AddHours(1).ToOADate());
                }
                else
                {
                    ResultsTable.Plot.SetAxisLimitsX(testResults[0].date.AddMinutes(-(lengthOfTests/5)).ToOADate(),
                        testResults[testResults.Count - 1].date.AddMinutes(lengthOfTests / 5).ToOADate());
                }
            }

            if (Configuration.ShowMinUp())
            {
                double minUp = Configuration.MinUp();
                var uploadHLineVector = ResultsTable.Plot.AddHorizontalLine(minUp);
                uploadHLineVector.Label = "Min. Upload";
                uploadHLineVector.Color = minUploadColor;
                uploadHLineVector.LineStyle = LineStyle.Dash;
            }

            if (Configuration.ShowMinDown())
            {
                double minDown = Configuration.MinDown();
                var downloadHLineVector = ResultsTable.Plot.AddHorizontalLine(minDown);
                downloadHLineVector.Label = "Min. Download";
                downloadHLineVector.Color = minDownloadColor;
                downloadHLineVector.LineStyle = LineStyle.Dash;
            }

            DrawMonthLines();
            RedrawAverages();
            CalculatePercentageBelowMinimums();
            AddVerticalSpan();
            ResultsTable.Plot.Legend();
            ResultsTable.Render();

            if(testResults.Count > 0)
            {
                DownloadSpeed.Text = testResults[testResults.Count - 1].downSpeed.ToString();
                UploadSpeed.Text = testResults[testResults.Count - 1].upSpeed.ToString();
            }
        }

        private void AddResult(BandwidthTest testResult, bool render = false)
        {
            downloadScatter.Add(testResult.date.ToOADate(), testResult.downSpeed);
            uploadScatter.Add(testResult.date.ToOADate(), testResult.upSpeed);

            if(testResult.date >= DateTime.Now.AddDays(-30))
            {
                downloadSpeeds.Add(testResult.downSpeed);
                uploadSpeeds.Add(testResult.upSpeed);
            }

            if (testResult.downSpeed < minimumDownload) countBelowMinDownload++;
            if (testResult.upSpeed < minimumUpload) countBelowMinUpload++;

            if (render)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    DrawMonthLines();
                    RedrawAverages();

                    CalculatePercentageBelowMinimums();
                    AddVerticalSpan();
                    ResultsTable.Render();

                    DownloadSpeed.Text = testResult.downSpeed.ToString();
                    UploadSpeed.Text = testResult.upSpeed.ToString();

                    if(viewer != null) viewer.wpfPlot1.Render();
                }));
            }
        }

        int numberOfMonthsEitherSideToDraw = 24;

        private void DrawMonthLines()
        {
            ScottPlot.Alignment[] alignments = (ScottPlot.Alignment[])Enum.GetValues(typeof(ScottPlot.Alignment));
            string[] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            if(testResults.Count > 1)
            {
                var firstDate = testResults.First().date;
                var startMonthLines = firstDate.AddMonths(-numberOfMonthsEitherSideToDraw);
                var lastDate = DateTime.Now;
                var endMonthLines = lastDate.AddMonths(numberOfMonthsEitherSideToDraw);

                for (DateTime month = new DateTime(startMonthLines.Year, startMonthLines.Month, 1); month < endMonthLines; month = month.AddMonths(1))
                {
                    var monthLine = ResultsTable.Plot.AddVerticalLine(x: month.ToOADate(), color: Color.DarkSlateGray, width: 1, style: LineStyle.Solid);

                    string monthText = months[month.Month -1];
                    if(month.Year != DateTime.Now.Year) monthText = month.Year.ToString() + Environment.NewLine + monthText;
                    var monthLabel =ResultsTable.Plot.AddText(monthText, month.ToOADate(), 0, size: 12, color: Color.DarkSlateGray);
                    monthLabel.Alignment = alignments[6];
                }
            }
        }

        private void RedrawAverages()
        {
            downloadAverageScatter.Clear();
            uploadAverageScatter.Clear();

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
                downloadAverageScatter.Add(avg.date.ToOADate(), avg.downSpeed);
                uploadAverageScatter.Add(avg.date.ToOADate(), avg.upSpeed);
            }
        }

        public static double Lerp(double start, double end, double ratio)
        {
            return start + (end - start) * ratio;
        }

        private void AddVerticalSpan()
        {
            if (Configuration.ShowDownloadRange())
            {
                double downloadRange = Configuration.DownloadRange();

                if (downloadSpeeds.Count > 3)
                {
                    int middle80Percent = (int)(downloadRange * downloadSpeeds.Count);

                    // Sort the list in ascending order
                    downloadSpeeds.Sort();

                    // Calculate the start and end indices for the middle 80% range
                    int startIndex = (downloadSpeeds.Count - middle80Percent) / 2;

                    // Get the temperature values for the middle 80% range
                    List<double> middle80PercentValues = downloadSpeeds.GetRange(startIndex, middle80Percent);

                    if (middle80PercentValues.Count > 1)
                    {
                        ResultsTable.Plot.Remove(vSpanDownload);
                        vSpanDownload = ResultsTable.Plot.AddVerticalSpan(middle80PercentValues.First(), middle80PercentValues.Last());
                        vSpanDownload.BorderColor = Color.Teal;
                        vSpanDownload.BorderLineStyle = LineStyle.None;
                        vSpanDownload.BorderLineWidth = 2;
                        vSpanDownload.Color = Color.FromArgb(75, Color.Aqua);
                        vSpanDownload.IsVisible = true;
                    }
                }
            }

            if (Configuration.ShowUploadRange())
            {
                double uploadRange = Configuration.UploadRange();

                if (uploadSpeeds.Count > 3)
                {
                    int middle80Percent = (int)(uploadRange * uploadSpeeds.Count);

                    // Sort the list in ascending order
                    uploadSpeeds.Sort();

                    // Calculate the start and end indices for the middle 80% range
                    int startIndex = (uploadSpeeds.Count - middle80Percent) / 2;

                    // Get the temperature values for the middle 80% range
                    List<double> middle80PercentValues = uploadSpeeds.GetRange(startIndex, middle80Percent);

                    if (middle80PercentValues.Count > 1)
                    {
                        ResultsTable.Plot.Remove(vSpanUpload);
                        vSpanUpload = ResultsTable.Plot.AddVerticalSpan(middle80PercentValues.First(), middle80PercentValues.Last());
                        vSpanUpload.BorderColor = Color.Red;
                        vSpanUpload.BorderLineStyle = LineStyle.None;
                        vSpanUpload.BorderLineWidth = 2;
                        vSpanUpload.Color = Color.FromArgb(75, Color.Red);
                        vSpanUpload.IsVisible = true;
                    }
                }
            }
        }

        private void CalculatePercentageBelowMinimums()
        {
            if (testResults.Count > 0)
            {
                DownloadMinPercentageValue.Text = (Math.Round(((double)countBelowMinDownload / testResults.Count) * 100, 1)).ToString() + "%";
                UploadMinPercentageValue.Text = (Math.Round(((double)countBelowMinUpload / testResults.Count) * 100, 1)).ToString() + "%";
            }
            else
            {
                DownloadMinPercentageValue.Text = "--";
                UploadMinPercentageValue.Text = "--";
            }
        }

        private void SetYAxisLimits(Plot plot, double highestYValue)
        {
            highestYValue += 2;
            if (testResults.Count == 0)
                plot.SetAxisLimitsY(0, 100);
            else
                plot.SetAxisLimitsY(0, highestYValue + 10 - (highestYValue % 10));
        }

        private static void DeleteFile(String fileToDelete)
        {
            var fi = new System.IO.FileInfo(fileToDelete);
            if (fi.Exists)
            {
                fi.Delete();
                fi.Refresh();
                while (fi.Exists)
                {
                    Thread.Sleep(100);
                    fi.Refresh();
                }
            }
        }

        //*--------------------- Event handlers ---------------------*//

        private void StartTest_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = null;
            StartTest.IsEnabled = false;
            thread = new Thread(new ThreadStart(() =>
            {
                BandwidthTest? test = Speedtester.RunSpeedTest();
                if (test != null)
                {
                    AddResult((BandwidthTest)test, true);
                    System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                    {
                        StartTest.IsEnabled = true;
                    }));
                }
            }));
            thread.Start();
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel.IsEnabled = false;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel file|*.xlsx";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            saveFileDialog.OverwritePrompt = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                if (File.Exists(saveFileDialog.FileName))
                    DeleteFile(saveFileDialog.FileName);

                string path = Environment.CurrentDirectory + "\\External\\speedtestsTemplate.xlsx";
                try
                {
                    File.Copy(path, saveFileDialog.FileName);

                    var excellApp = new Microsoft.Office.Interop.Excel.Application();

                    excellApp.DisplayAlerts = false;
                    excellApp.ScreenUpdating = false;

                    var wb = excellApp.Workbooks.Open(saveFileDialog.FileName);
                    Worksheet ws = wb.ActiveSheet;

                    int row = 2;
                    while (ws.Cells[row, 1].Value != null)
                        row++;


                    //while (ws.GetCellAt(row, 0).ToString() != "")
                    //    row++;

                    foreach(var result in testResults)
                    {
                        ws.Cells[row, 1].Value = result.date;
                        ws.Cells[row, 2].Value = result.downSpeed;
                        ws.Cells[row, 3].Value = result.upSpeed;

                        //ws.SetCellValue(row, 0, result.date);
                        //ws.SetCellValue(row, 1, result.downSpeed);
                        //ws.SetCellValue(row, 2, result.upSpeed);
                        row++;
                    }

                    wb.SaveAs(saveFileDialog.FileName);
                    wb.Close();
                    excellApp.Quit();
                    //wb.SaveAs(saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "" + ex.InnerException.Message, "");
                }
            }
            ExportToExcel.IsEnabled = true;
        }

        private void DeployMainWindowMenu(object sender, EventArgs e)
        {
            MenuItem openInNewWindow = new MenuItem() { Header = "Open in new window" };
            openInNewWindow.Click += OpenInNewWindow;

            ContextMenu rightClickMenu = new ContextMenu();
            rightClickMenu.Items.Add(openInNewWindow);

            rightClickMenu.IsOpen = true;
        }

        public void DeploySaveImageMenu(object sender, EventArgs e)
        {
            var cm = new ContextMenu();

            MenuItem SaveImageMenuItem = new MenuItem() { Header = "Save Image" };
            SaveImageMenuItem.Click += RightClickMenuSaveImageClick;
            cm.Items.Add(SaveImageMenuItem);

            cm.IsOpen = true;
        }

        private void RightClickMenuSaveImageClick(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                FileName = "Speedtest Results.png",
                Filter = "PNG Files (*.png)|*.png;*.png" +
                         "|JPG Files (*.jpg, *.jpeg)|*.jpg;*.jpeg" +
                         "|BMP Files (*.bmp)|*.bmp;*.bmp" +
                         "|All files (*.*)|*.*"
            };

            if (sfd.ShowDialog() is true)
                resultsTableCopy.SaveFig(sfd.FileName);
        }

        private void OpenInNewWindow(object sender, RoutedEventArgs e)
        {
            if (viewer != null) viewer.Close();
            resultsTableCopy = ResultsTable.Plot.Copy();
            resultsTableCopy.YLabel("Speed (mbps)");
            resultsTableCopy.Style(ScottPlot.Style.Gray2);
            resultsTableCopy.Style(figureBackground: System.Drawing.Color.FromArgb(7, 38, 59));
            resultsTableCopy.XAxis.DateTimeFormat(true);
            resultsTableCopy.SetAxisLimitsY(0, highestYValue + 10 - (highestYValue % 10));
            viewer = new WpfPlotViewer(resultsTableCopy);

            viewer.wpfPlot1.Configuration.AllowDroppedFramesWhileDragging = true;
            viewer.wpfPlot1.Configuration.LockVerticalAxis = true;
            viewer.wpfPlot1.RightClicked -= ResultsTable.DefaultRightClickEvent;
            viewer.wpfPlot1.RightClicked += DeploySaveImageMenu;
            viewer.Show();
        }
    }
}
