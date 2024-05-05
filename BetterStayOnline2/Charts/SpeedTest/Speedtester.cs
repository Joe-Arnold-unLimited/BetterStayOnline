using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using Microsoft.WindowsAPICodePack.Net;
using static BetterStayOnline2.Charts.PlotManager;
using System.Net.NetworkInformation;
using System.Linq;
using BetterStayOnline2.Events;
using System.Collections.ObjectModel;
using System.Windows.Shapes;

namespace BetterStayOnline2.Charts
{
    internal static class Speedtester
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
#if !DEBUG
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif
                process.StartInfo.FileName = dir + "\\Charts\\SpeedTest\\speedtest.exe";

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
                    process.StartInfo.FileName = dir + "\\Charts\\SpeedTest\\speedtest.exe";

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

                // Get active network
                NetworkCollection networks = NetworkListManager.GetNetworks(NetworkConnectivityLevels.Connected);
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                var activeAdapter = networkInterfaces.First(networkInterface => networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback
                    && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                    && networkInterface.OperationalStatus == OperationalStatus.Up
                    && networkInterface.Name.StartsWith("vEthernet") == false);

                var activeNetwork = networks.FirstOrDefault(network =>
                {
                    if (network.Connections.Any(conn => conn.AdapterId == Guid.Parse(activeAdapter.Id)))
                    {
                        return true;
                    }
                    return false;
                });

                string activeNetworkName = activeNetwork != null ? activeNetwork.Name : "";
                bandwidthTest.networkName = activeNetworkName;

                // Catch data from Speedtest CLI
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
                    if (words[0].Contains("isp"))
                        try
                        {
                            bandwidthTest.isp = words[1].ToUpper();
                        }
                        catch (Exception) { }
                }

#if !DEBUG
                // Save result to json
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\External\\testresults.json";

                try
                {
                    using (var scope = new TransactionScope())
                    {
                        try
                        {
                            // Read existing data
                            string existingData = File.ReadAllText(path);
                            JObject obj = JObject.Parse(existingData);
                            JArray jsonTestResults = (JArray)obj.GetValue("TestResults");

                            // Add new test data
                            JObject newTest = new JObject
                            {
                                { "DateTime", bandwidthTest.date.ToString("dd/MM/yyyy hh:mm tt") },
                                { "Download", bandwidthTest.downSpeed },
                                { "Upload", bandwidthTest.upSpeed },
                                { "NetworkName", bandwidthTest.networkName },
                                { "ISP", bandwidthTest.isp }
                            };

                            jsonTestResults.Add(newTest);
                            obj["TestResults"] = jsonTestResults;

                            // Validate updated JSON
                            try
                            {
                                JToken.Parse(obj.ToString());
                            }
                            catch (JsonReaderException)
                            {
                                Console.WriteLine("Updated JSON is not valid. Aborting update.");
                                return bandwidthTest;
                            }
                            finally
                            {
                                // Perform atomic write to the file
                                string tempPath = path + ".temp";
                                File.WriteAllText(tempPath, obj.ToString());
                                File.Replace(tempPath, path, null);

                                scope.Complete(); // Commit the transaction
                            }
                        }
                        catch (Exception ex)
                        {
                            // Handle exceptions or log errors
                            Console.WriteLine($"Error processing speed test: {ex.Message}");
                        }
                    }
                }
                catch (TransactionAbortedException)
                {
                    // Transaction was rolled back
                    Console.WriteLine("Transaction aborted. File not modified.");
                }
#endif
                return bandwidthTest;
            }
            return null;
        }
    }
}