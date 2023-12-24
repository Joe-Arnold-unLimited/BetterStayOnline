using BetterStayOnline.Model;
using BetterStayOnline.MVVM.Model;
using BetterStayOnline.MVVM.ViewModel;
using BetterStayOnline.SpeedTest;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Color = System.Drawing.Color;
using MenuItem = System.Windows.Controls.MenuItem;

namespace BetterStayOnline.MVVM.View
{
    /// <summary>
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        List<Timer> eventTimers;
        Func<ResultsView, bool> redraw = (r) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                r.plotManager.UpdatePlot();
                PlotManager.SecondWindow.UpdatePlot();
            });
            return true;
        };

        PlotManager plotManager;

        private void RenderPlot(Plot plot, WpfPlot wpfPlot, BandwidthTest lastResult)
        {
            CalculatePercentageBelowMinimums();

            DownloadSpeed.Text = lastResult.downSpeed.ToString();
            UploadSpeed.Text = lastResult.upSpeed.ToString();
        }

        public ResultsView()
        {
            InitializeComponent();

            var secondWindow = PlotManager.SecondWindow;

            // Subscribe to plot update event
            secondWindow.OnPlotUpdate += RenderPlot;

            // Access the plot from PlotManager
            var secondWindowPlot = secondWindow.plot;

            // Update the plot when needed
            secondWindow.UpdatePlot();

            plotManager = new PlotManager(ResultsTable.Plot,
                Configuration.ShowDownloadPoints(), Configuration.ShowUploadPoints(),
                Configuration.ShowDownloadTrendline(), Configuration.ShowUploadTrendline(),
                Configuration.ShowMinDown(), Configuration.ShowMinUp(),
                Configuration.ShowDownloadCandles(), Configuration.ShowUploadCandles(),
                Configuration.MinDown(), Configuration.MinUp(),
                ResultsTable);

            ResultsTable.RightClicked -= ResultsTable.DefaultRightClickEvent;
            ResultsTable.RightClicked += DeployMainWindowMenu;
            ResultsTable.Plot.SetAxisLimitsY(0, 100);
            ResultsTable.Configuration.LockVerticalAxis = true;

            int minimumDownload = Configuration.MinDown();
            int minimumUpload = Configuration.MinUp();

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

            plotManager.UpdatePlot();

            eventTimers = new List<Timer>();
            eventTimers = TimerFactory.CreateTimers(eventTimers, EventReader.GetEvents(), redraw, this).ToList();
        }

        private void CalculatePercentageBelowMinimums()
        {
            if (PlotManager.testResults.Count > 0)
            {
                var x = (double)(PlotManager.testResults.Select(result => result.downSpeed).Where(result => result < Configuration.MinDown()).Count());
                var y = (double)(PlotManager.testResults.Select(result => result.upSpeed).Where(result => result < Configuration.MinUp()).Count());

                DownloadMinPercentageValue.Text = Math.Round(x / PlotManager.testResults.Count * 100, 1).ToString() + "%";
                UploadMinPercentageValue.Text = Math.Round(y / PlotManager.testResults.Count * 100, 1).ToString() + "%";
            }
            else
            {
                DownloadMinPercentageValue.Text = "--";
                UploadMinPercentageValue.Text = "--";
            }
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
                    PlotManager.testResults.Add((BandwidthTest)test);
                    //plotManager.AddResult((BandwidthTest)test);
                    //PlotManager.SecondWindow.AddResult((BandwidthTest)test);
                    System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                    {
                        StartTest.IsEnabled = true;
                        plotManager.UpdatePlot();
                        PlotManager.SecondWindow.UpdatePlot();
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

                    foreach(var result in PlotManager.testResults)
                    {
                        ws.Cells[row, 1].Value = result.date;
                        ws.Cells[row, 2].Value = result.downSpeed;
                        ws.Cells[row, 3].Value = result.upSpeed;
                        row++;
                    }

                    wb.SaveAs(saveFileDialog.FileName);
                    wb.Close();
                    excellApp.Quit();
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
            MenuItem SaveImageMenuItem = new MenuItem() { Header = "Save Image" };
            SaveImageMenuItem.Click += RightClickMenuSaveImageClick;

            ContextMenu rightClickMenu = new ContextMenu();
            rightClickMenu.Items.Add(SaveImageMenuItem);

            rightClickMenu.IsOpen = true;
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
                plotManager.plot.SaveFig(sfd.FileName);
        }

        private void OpenInNewWindow(object sender, RoutedEventArgs e)
        {
            if (PlotManager.SecondWindow.viewer != null) PlotManager.SecondWindow.viewer.Close();

            var resultsTableCopy = ResultsTable.Plot.Copy();

            PlotManager.SecondWindow = new PlotManager(resultsTableCopy, 
                Configuration.ShowDownloadPoints(), Configuration.ShowUploadPoints(),
                Configuration.ShowDownloadTrendline(), Configuration.ShowUploadTrendline(),
                Configuration.ShowMinDown(), Configuration.ShowMinUp(),
                Configuration.ShowDownloadCandles(), Configuration.ShowUploadCandles(),
                Configuration.MinDown(), Configuration.MinUp());

            PlotManager.SecondWindow.viewer = new WpfPlotViewer(resultsTableCopy, 1200, 660);
            PlotManager.SecondWindow.DrawViewer(plotManager);

            PlotManager.SecondWindow.viewer.wpfPlot1.Configuration.AllowDroppedFramesWhileDragging = true;
            PlotManager.SecondWindow.viewer.wpfPlot1.Configuration.LockVerticalAxis = true;
            PlotManager.SecondWindow.viewer.wpfPlot1.RightClicked -= ResultsTable.DefaultRightClickEvent;
            PlotManager.SecondWindow.viewer.wpfPlot1.RightClicked += DeploySaveImageMenu;
            PlotManager.SecondWindow.viewer.Show();
        }
    }
}
