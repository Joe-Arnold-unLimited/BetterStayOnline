using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BetterStayOnline2.Charts;
using BetterStayOnline2.Core;
using BetterStayOnline2.MVVM.ViewModel;
using Newtonsoft.Json.Linq;
using ScottPlot;
using ScottPlot.Control;
using ScottPlot.Plottable;
using ScottPlot.Plottables;
using ScottPlot.WPF;

namespace BetterStayOnline2.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();

            PlotManager.ResultsTable = ResultsTable;

            CreateTable();
        }

        private void CreateTable()
        {
            PlotManager.DrawTable();

            PlotManager.UpdatePlot();
        }
    }
}