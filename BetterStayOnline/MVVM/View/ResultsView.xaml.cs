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

namespace BetterStayOnline.MVVM.View
{
    /// <summary>
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        private JArray jsonTestResults = new JArray();
        public List<BandwidthTest> testResults = new List<BandwidthTest>();

        private ScatterPlotList<double> downloadScatter;
        private ScatterPlotList<double> uploadScatter;

        private int minimumDownload;
        private int minimumUpload;
        private int countBelowMinDownload;
        private int countBelowMinUpload;

        List<Timer> eventTimers;
        Func<ResultsView, bool> redraw = (r) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                r.ReadPreexistingData();
                r.testResults = r.testResults.OrderBy(x => x.date).ToList();

                r.downloadScatter.Clear();
                r.uploadScatter.Clear();

                double highestYValue = 0;
                foreach (var testResult in r.testResults)
                {
                    r.AddResult(testResult);
                    if (testResult.downSpeed > highestYValue) highestYValue = testResult.downSpeed;
                    if (testResult.upSpeed > highestYValue) highestYValue = testResult.upSpeed;
                }
                r.SetYAxisLimits(highestYValue);

                r.CalculatePercentageBelowMinimums();
                r.ResultsTable.Render();
            });
            return true;
        }; 

        public ResultsView()
        {
            InitializeComponent();

            ReadPreexistingData();
            ResultsTable.Plot.YLabel("Speed (mbps)");
            ResultsTable.Plot.Style(ScottPlot.Style.Gray2);
            ResultsTable.Plot.Style(figureBackground: System.Drawing.Color.FromArgb(7, 38, 59));
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
            catch(Exception)
            {
                int x = 0;
            }
        }

        private void SetUpTable()
        {
            ResultsTable.Plot.Clear();
            testResults = testResults.OrderBy(x => x.date).ToList();

            downloadScatter = ResultsTable.Plot.AddScatterList();
            downloadScatter.Label = "Download";
            downloadScatter.MarkerSize = 6;
            downloadScatter.LineWidth = 2;
            uploadScatter = ResultsTable.Plot.AddScatterList();
            uploadScatter.Label = "Upload";
            uploadScatter.MarkerSize = 6;
            uploadScatter.LineWidth = 2;

            bool moreThan31DaysOfResults = false;
            double highestYValue = 0;
            foreach (var testResult in testResults)
            {
                AddResult(testResult);

                if (!moreThan31DaysOfResults && testResult.date < DateTime.Now.AddDays(-31)) moreThan31DaysOfResults = true;
                if (testResult.downSpeed > highestYValue) highestYValue = testResult.downSpeed;
                if (testResult.upSpeed > highestYValue) highestYValue = testResult.upSpeed;
            }

            SetYAxisLimits(highestYValue);

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

            if (Configuration.ShowMinDown())
            {
                double minDown = Configuration.MinDown();
                var downloadHLineVector = ResultsTable.Plot.AddHorizontalLine(minDown);
                downloadHLineVector.Label = "Min. Download";
                downloadHLineVector.Color = Color.DeepSkyBlue;
                downloadHLineVector.LineStyle = ScottPlot.LineStyle.Dash;
            }

            if (Configuration.ShowMinUp())
            {
                double minUp = Configuration.MinUp();
                var uploadHLineVector = ResultsTable.Plot.AddHorizontalLine(minUp);
                uploadHLineVector.Label = "Min. Upload";
                uploadHLineVector.Color = Color.Orange;
                uploadHLineVector.LineStyle = ScottPlot.LineStyle.Dash;
            }

            CalculatePercentageBelowMinimums();
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

            if (testResult.downSpeed < minimumDownload) countBelowMinDownload++;
            if (testResult.upSpeed < minimumUpload) countBelowMinUpload++;

            if (render)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    CalculatePercentageBelowMinimums();
                    ResultsTable.Render();

                    DownloadSpeed.Text = testResult.downSpeed.ToString();
                    UploadSpeed.Text = testResult.upSpeed.ToString();
                }));
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

        private void SetYAxisLimits(double highestYValue)
        {
            highestYValue += 2;
            if (testResults.Count == 0)
                ResultsTable.Plot.SetAxisLimitsY(0, 100);
            else
                ResultsTable.Plot.SetAxisLimitsY(0, highestYValue + 10 - (highestYValue % 10));
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
    }
}
