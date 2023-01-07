﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BetterStayOnline.MVVM.Model;
using BetterStayOnline.MVVM.ViewModel;
using ScottPlot;

namespace BetterStayOnline.MVVM.View
{
    /// <summary>
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        private List<BandWidthTest> testResults = new List<BandWidthTest>();

        struct BandWidthTest
        {
            public DateTime date;
            public double downSpeed;
            public double upSpeed;
        }

        public ResultsView()
        {
            InitializeComponent();
            ResultsTable.Plot.YLabel("Speed (mbps)");
            ResultsTable.Plot.Style(ScottPlot.Style.Blue1);
            ResultsTable.Plot.Style(figureBackground: System.Drawing.Color.FromArgb(7, 38, 59));
            ResultsTable.Plot.XAxis.DateTimeFormat(true);

            //ResultsTable.Plot.SetAxisLimitsY(0, 70);

        }

        // Publicly facing for adding single results
        public void AddResult(DateTime time, double downSpeed, double upSpeed, bool plot = true)
        {
            BandWidthTest newTest = new BandWidthTest();

            newTest.date = time;
            newTest.downSpeed = downSpeed;
            newTest.upSpeed = upSpeed;

            AddResult(newTest, plot);
        }

        private void AddResult(BandWidthTest testResult, bool plot = false)
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

            bool past30Days = false;
            double highestYValue = 0;
            foreach(var testResult in testResults)
            {
                dates.Add(testResult.date);
                downSpeeds.Add(testResult.downSpeed);
                upSpeeds.Add(testResult.upSpeed);

                if (!past30Days && testResult.date > DateTime.Now.AddDays(-30)) past30Days = true;
                if (past30Days)
                {
                    if (testResult.downSpeed > highestYValue) highestYValue = testResult.downSpeed;
                    if (testResult.upSpeed > highestYValue) highestYValue = testResult.upSpeed;
                }
            }

            ResultsTable.Plot.AddScatter(dates.ToArray().Select(x => x.ToOADate()).ToArray(), downSpeeds.ToArray());
            ResultsTable.Plot.AddScatter(dates.ToArray().Select(x => x.ToOADate()).ToArray(), upSpeeds.ToArray());

            ResultsTable.Plot.SetAxisLimitsY(0, highestYValue + 10 - (highestYValue % 10));
            ResultsTable.Refresh();
        }
    }
}
