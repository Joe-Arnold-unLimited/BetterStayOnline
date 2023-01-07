using BetterStayOnline.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BetterStayOnline
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ResultList results = new ResultList();

            results.AddResult(new DateTime(2022, 12, 19, 1, 1, 1), 42, 14);
            results.AddResult(new DateTime(2022, 12, 25, 1, 1, 1), 54, 18);
            results.AddResult(new DateTime(2022, 12, 28, 1, 1, 1), 48, 16);
            results.AddResult(new DateTime(2023, 1, 1, 1, 1, 1), 58, 15);
            results.AddResult(new DateTime(2023, 1, 3, 1, 1, 1), 61, 12);
            results.AddResult(new DateTime(2023, 1, 6, 1, 1, 1), 49, 21);

            base.OnStartup(e);
        }
    }
}
