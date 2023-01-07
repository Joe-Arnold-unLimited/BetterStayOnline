using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScottPlot;
using BetterStayOnline.Core;
using BetterStayOnline.MVVM.Model;
using BetterStayOnline.MVVM.View;
using BetterStayOnline.SpeedTest;
using System.Threading;
using System.Windows;
using System.Text.RegularExpressions;

namespace BetterStayOnline.MVVM.ViewModel
{
    class ResultsViewModel : ObservableObject
    {
        public RelayCommand StartTestCommand { get; set; }

        private string _downloadSpeed;
        public string DownloadSpeed
        {
            get
            {
                return _downloadSpeed;
            }
            set
            {
                _downloadSpeed = value;
                OnPropertyChanged(nameof(DownloadSpeed));
            }
        }

        private string _uploadSpeed;
        public string UploadSpeed
        {
            get
            {
                return _uploadSpeed;
            }
            set
            {
                _uploadSpeed = value;
                OnPropertyChanged(nameof(UploadSpeed));
            }
        }

        public WpfPlot ResultsTable { get; set; }


        private List<BandWidthTest> testResults = new List<BandWidthTest>();

        struct BandWidthTest
        {
            public DateTime date;
            public double downSpeed;
            public double upSpeed;
        }


        public ResultsViewModel()
        {
            ResultsTable = new WpfPlot();
            ResultsTable.Plot.YLabel("Speed (mbps)");
            ResultsTable.Plot.Style(ScottPlot.Style.Blue1);
            ResultsTable.Plot.Style(figureBackground: System.Drawing.Color.FromArgb(7, 38, 59));
            ResultsTable.Plot.XAxis.DateTimeFormat(true);
            ResultsTable.Plot.SetAxisLimitsY(0, 100);

            ResultsTable.Refresh();

            StartTestCommand = new RelayCommand(o =>
            {
                Thread thread = null;
                thread = new Thread(new ThreadStart(() =>
                {
                    string output = Speedtester.RunSpeedTest();
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    output = regex.Replace(output, " ");

                    string[] lines = output.ToLower().Split(new[] { '\r', '\n' });

                    double down = 0, up = 0;
                    foreach (var line in lines)
                    {
                        string[] words = line.Trim().Split(' ');
                        if (words[0].Contains("download"))
                            try
                            {
                                down = double.Parse(words[1]);
                                DownloadSpeed = words[1];
                            }
                            catch (Exception) { }
                        if (words[0].Contains("upload"))
                            try
                            {
                                up = double.Parse(words[1]);
                                UploadSpeed = words[1];
                            }
                            catch (Exception) { }
                    }

                    AddResult(DateTime.Now, down, up, true);
                }));
                thread.Start();
            });
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
            foreach (var testResult in testResults)
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

            try
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    ResultsTable.Refresh();
                }));
            }
            catch (Exception) { }
        }
    }
}
