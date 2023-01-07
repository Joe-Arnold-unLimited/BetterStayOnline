using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterStayOnline.SpeedTest
{
    public static class Speedtester
    {
        public static void SaveNewResult(DateTime dateTime, double download, double upload)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\External\\testresults.json";
            string data = File.ReadAllText(path);
            JObject obj = JObject.Parse(data);
            JArray jsonTestResults = (JArray)obj.GetValue("TestResults");

            JObject newTest = new JObject();
            newTest.Add("DateTime", dateTime.ToString("dd/MM/yyyy hh:mm tt"));
            newTest.Add("Download", download);
            newTest.Add("Upload", upload);

            jsonTestResults.Add(newTest);
            obj = new JObject();
            obj.Add("TestResults", jsonTestResults);

            File.WriteAllText(path, obj.ToString());
        }

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
