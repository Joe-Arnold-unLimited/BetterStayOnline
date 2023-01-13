using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Office.Interop.Excel;
using BetterStayOnline.SpeedTest;
using System.IO;
using Newtonsoft.Json.Linq;
using BetterStayOnline.MVVM.Model;
using ScottPlot.Plottable;

namespace BetterStayOnline.MVVM.View
{
    /// <summary>
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        private JArray jsonTestResults = new JArray();
        private List<BandwidthTest> testResults = new List<BandwidthTest>();

        private ScatterPlotList<double> downloadScatter;
        private ScatterPlotList<double> uploadScatter;

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

            SetUpTable();
        }

        private void ReadPreexistingData()
        {
            try
            {
                string data = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\External\\testresults.json");
                JObject obj = JObject.Parse(data);
                jsonTestResults = (JArray)obj.GetValue("TestResults");

                foreach(var result in jsonTestResults)
                {
                    BandwidthTest newTest = new BandwidthTest();
                    newTest.date = DateTime.Parse((string)((JObject)result).GetValue("DateTime"));
                    newTest.downSpeed = double.Parse((string)((JObject)result).GetValue("Download"));
                    newTest.upSpeed = double.Parse((string)((JObject)result).GetValue("Upload"));
                    testResults.Add(newTest);
                }
            }
            catch(Exception e)
            {
                int x = 0;
            }
        }

        private void SetUpTable()
        {
            ResultsTable.Plot.Clear();
            testResults = testResults.OrderBy(x => x.date).ToList();

            downloadScatter = ResultsTable.Plot.AddScatterList();
            uploadScatter = ResultsTable.Plot.AddScatterList();

            bool moreThan31DaysOfResults = false;
            double highestYValue = 0;
            foreach (var testResult in testResults)
            {
                AddResult(testResult);

                if (!moreThan31DaysOfResults && testResult.date < DateTime.Now.AddDays(-31)) moreThan31DaysOfResults = true;
                if (testResult.downSpeed > highestYValue) highestYValue = testResult.downSpeed;
                if (testResult.upSpeed > highestYValue) highestYValue = testResult.upSpeed;
            }

            ResultsTable.Plot.SetAxisLimitsY(0, highestYValue + 10 - (highestYValue % 10));

            if (moreThan31DaysOfResults || testResults.Count <= 1)
                ResultsTable.Plot.SetAxisLimitsX(DateTime.Now.AddDays(-32).ToOADate(), testResults[testResults.Count - 1].date.AddDays(1).ToOADate());
            else
            {
                double lengthOfTests = (testResults[testResults.Count - 1].date - testResults[0].date).TotalMinutes;
                if (lengthOfTests < TimeSpan.FromHours(6).TotalMinutes)
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

            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ResultsTable.Render();

                    DownloadSpeed.Text = testResults[testResults.Count - 1].downSpeed.ToString();
                    UploadSpeed.Text = testResults[testResults.Count - 1].upSpeed.ToString();
                }));
            }
            catch (Exception) { }
        }

        private void AddResult(BandwidthTest testResult, bool render = false)
        {
            downloadScatter.Add(testResult.date.ToOADate(), testResult.downSpeed);
            uploadScatter.Add(testResult.date.ToOADate(), testResult.upSpeed);

            if (render)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ResultsTable.Render();

                    DownloadSpeed.Text = testResult.downSpeed.ToString();
                    UploadSpeed.Text = testResult.upSpeed.ToString();
                }));
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
                if(test != null)
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
    }
}
