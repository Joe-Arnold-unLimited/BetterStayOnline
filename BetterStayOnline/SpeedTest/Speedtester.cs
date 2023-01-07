using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterStayOnline.SpeedTest
{
    public static class Speedtester
    {
        public static string RunSpeedTest()
        {
            Process process = null;
            string output = null;

            try
            {
                string dir = Environment.CurrentDirectory;
                process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                process.StartInfo.FileName = dir + "\\SpeedTest\\speedtest.exe";

                process.Start();

                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                int x = 0;
                throw;
            }
            finally
            {
                if (process != null) process.Dispose();
            }

            return output;
        }
    }
}
