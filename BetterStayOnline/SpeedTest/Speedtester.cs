using BetterStayOnline.MVVM.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BetterStayOnline.SpeedTest
{
    public static class Speedtester
    {
        public static BandwidthTest RunSpeedTest()
        {
            // Run test
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

            // Read test results
            BandwidthTest bandwidthTest = new BandwidthTest();
            bandwidthTest.date = DateTime.Now;

            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            output = regex.Replace(output, " ");

            string[] lines = output.ToLower().Split(new[] { '\r', '\n' });

            foreach (var line in lines)
            {
                string[] words = line.Trim().Split(' ');
                if (words[0].Contains("download"))
                    try
                    {
                        bandwidthTest.downSpeed = double.Parse(words[1]);
                    }
                    catch (Exception) { }
                if (words[0].Contains("upload"))
                    try
                    {
                        bandwidthTest.upSpeed = double.Parse(words[1]);
                    }
                    catch (Exception) { }
            }

            // Save result to json
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\External\\testresults.json";
            string data = File.ReadAllText(path);
            JObject obj = JObject.Parse(data);
            JArray jsonTestResults = (JArray)obj.GetValue("TestResults");

            JObject newTest = new JObject();
            newTest.Add("DateTime", bandwidthTest.date.ToString("dd/MM/yyyy hh:mm tt"));
            newTest.Add("Download", bandwidthTest.downSpeed);
            newTest.Add("Upload", bandwidthTest.upSpeed);

            jsonTestResults.Add(newTest);
            obj = new JObject();
            obj.Add("TestResults", jsonTestResults);

            File.WriteAllText(path, obj.ToString());
            return bandwidthTest;
        }
    }
}
