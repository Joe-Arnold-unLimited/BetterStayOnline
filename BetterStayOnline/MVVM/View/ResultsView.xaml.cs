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

namespace BetterStayOnline.MVVM.View
{
    /// <summary>
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        private JArray jsonTestResults = new JArray();
        private List<BandwidthTest> testResults = new List<BandwidthTest>();

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

            if (testResults.Count > 0) PlotGraph();
            ResultsTable.Refresh();
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

        //https://coderwall.com/p/app3ya/read-excel-file-in-c
        //private string SaveToExcel()
        //{
        //}

        private void AddResult(BandwidthTest testResult, bool plot = false)
        {
            testResults.Add(testResult);
            testResults.OrderBy(x => x.date);

            if (plot) PlotGraph();
        }

        private void PlotGraph()
        {
            ResultsTable.Plot.Clear();

            List<DateTime> dates = new List<DateTime>();
            List<double> downSpeeds = new List<double>();
            List<double> upSpeeds = new List<double>();

            bool moreThan30DaysOfResults = false;
            bool moreThan7Days = false;
            double highestYValue = 0;
            foreach (var testResult in testResults)
            {
                dates.Add(testResult.date);
                downSpeeds.Add(testResult.downSpeed);
                upSpeeds.Add(testResult.upSpeed);

                if (!moreThan30DaysOfResults && testResult.date < DateTime.Now.AddDays(-30)) moreThan30DaysOfResults = true;
                if (!moreThan30DaysOfResults && !moreThan7Days && testResult.date < DateTime.Now.AddDays(-7)) moreThan7Days = true;
                if (testResult.downSpeed > highestYValue) highestYValue = testResult.downSpeed;
                if (testResult.upSpeed > highestYValue) highestYValue = testResult.upSpeed;
            }

            ResultsTable.Plot.AddScatter(dates.ToArray().Select(x => x.ToOADate()).ToArray(), downSpeeds.ToArray());
            ResultsTable.Plot.AddScatter(dates.ToArray().Select(x => x.ToOADate()).ToArray(), upSpeeds.ToArray());

            ResultsTable.Plot.SetAxisLimitsY(0, highestYValue + 10 - (highestYValue % 10));

            if(moreThan30DaysOfResults)
                ResultsTable.Plot.SetAxisLimitsX(DateTime.Now.AddDays(-32).ToOADate(), testResults[testResults.Count - 1].date.AddDays(1).ToOADate());
            else if(!moreThan7Days)
                ResultsTable.Plot.SetAxisLimitsX(DateTime.Now.AddDays(-7).ToOADate(), testResults[testResults.Count - 1].date.AddDays(1).ToOADate());

            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {//this refer to form in WPF application 
                    ResultsTable.Refresh();
                }));
            }
            catch (Exception) { }
        }

        //*--------------------- Event handlers ---------------------*//

        private void StartTest_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = null;
            StartTest.IsEnabled = false;
            thread = new Thread(new ThreadStart(() =>
            {
                AddResult(Speedtester.RunSpeedTest(), true);
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    StartTest.IsEnabled = true;
                }));
            }));
            thread.Start();
        }
    }
}
