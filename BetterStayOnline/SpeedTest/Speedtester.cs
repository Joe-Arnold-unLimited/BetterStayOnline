using BetterStayOnline.MVVM.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterStayOnline.SpeedTest
{
    public static class Speedtester
    {
        // This uses speedtester.exe which CANNOT be used for commercial purposes
        // TODO: Find alternative
        public static BandwidthTest? RunSpeedTest()
        {
            // Run test
            Process process = null;
            string output = null;
            bool firstTime = false;

            try
            {
                string dir = Environment.CurrentDirectory;
                process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.FileName = dir + "\\SpeedTest\\speedtest.exe";

                process.Start();

                StreamReader sr = process.StandardError;
                StringBuilder input = new StringBuilder();

                if (!process.StandardOutput.EndOfStream)
                {
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }
                else
                {
                    // This is the first time, open to accept licence agreement
                    process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                    process.StartInfo.RedirectStandardInput = false;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.FileName = dir + "\\SpeedTest\\speedtest.exe";

                    process.Start();
                    process.WaitForExit();

                    firstTime = true;
                }
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                if (process != null) process.Dispose();
            }

            if (!firstTime)
            {
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
            return null;
        }
    }
}