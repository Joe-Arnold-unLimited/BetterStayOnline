using BetterStayOnline2.Charts;
using BetterStayOnline2.Events;
using BetterStayOnline2.MVVM.ViewModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterStayOnline2.ConnectionLoss
{
    public class DownDetector
    {
        public DownDetector()
        {
            // Subscribe to the NetworkAvailabilityChanged event
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private bool recordThisNextNetworkChange = true;

        // Event handler method for NetworkAvailabilityChanged event
        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                if (recordThisNextNetworkChange)
                {
                    PlotManager.NetworkConnectionEstablished();
                }
            }
            else
            {
                // Don't record network losses when asleep
                if (!isAsleep)
                {
                    recordThisNextNetworkChange = true;
                    PlotManager.NetworkConnectionLost();
                }
                else
                {
                    recordThisNextNetworkChange = false;
                }
            }
        }

        private bool isAsleep = false;

        // Event handler to listen for machine going to sleep
        private void  OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                isAsleep = true;
            }
            else if(e.Mode == PowerModes.Resume)
            {
                isAsleep = false;
            }
        }
    }
}
