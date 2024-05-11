using BetterStayOnline2.Charts;
using BetterStayOnline2.Core;
using BetterStayOnline2.Editing;
using BetterStayOnline2.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline2.MVVM.ViewModel
{
    internal class EditViewModel : ObservableObject
    {
        public RelayCommand RemoveEditableResultCommand { get; set; }

        private ObservableCollection<EditableResult> _editableResults;
        public ObservableCollection<EditableResult> EditableResults
        {
            get { return _editableResults; }
            set
            { 
                _editableResults = value;
                OnPropertyChanged();
            }
        }

        public EditViewModel() 
        {
            EditableResults = new ObservableCollection<EditableResult>();

            RemoveEditableResultCommand = new RelayCommand(o =>
            {
                foreach (var e in _editableResults.Where(e => e.Selected).ToList())
                {
                    PlotManager.RemoveDatapoint(e.DownSpeed, e.UpSpeed, e.Datetime.ToOADate(), e.NetworkName, e.ISP);
                }
            });
        }
    }
}