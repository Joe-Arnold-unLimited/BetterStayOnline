﻿using BetterStayOnline2.Charts;
using BetterStayOnline2.Core;
using BetterStayOnline2.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace BetterStayOnline2.MVVM.View
{
    /// <summary>
    /// Interaction logic for EventsView.xaml
    /// </summary>
    public partial class EventsView : UserControl
    {
        public EventsView()
        {
            InitializeComponent();
        }

        // When row is clicked, select the checkbox
        private void DataGridRow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is CheckBox)
                return; // Let the CheckBox handle the click

            if (sender is DataGridRow row)
            {
                e.Handled = true;
                Event rowData = (Event)row.Item;
                rowData.Selected = !rowData.Selected; // Toggle Selected property
            }
        }
    }
}
