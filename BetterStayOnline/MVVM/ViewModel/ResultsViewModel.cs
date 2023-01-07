using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScottPlot;
using BetterStayOnline.Core;
using BetterStayOnline.MVVM.Model;

namespace BetterStayOnline.MVVM.ViewModel
{
    class ResultsViewModel : ObservableObject
    {
        public readonly ResultList _results;

        public ResultsViewModel(ResultList results)
        {
            _results = results;
        }
    }
}
