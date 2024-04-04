using BetterStayOnline2.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace BetterStayOnline2.ConnectionLoss
{
    public class DownDetector
    {
        public DownDetector()
        {
            // Subscribe to the NetworkAvailabilityChanged event
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        // Event handler method for NetworkAvailabilityChanged event
        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                PlotManager.NetworkConnectionEstablished();
            }
            else
            {
                PlotManager.NetworkConnectionLost();
            }
        }
    }
}
