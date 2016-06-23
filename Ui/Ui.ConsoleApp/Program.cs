﻿namespace codingfreaks.pping.Ui.ConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading;

    using codingfreaks.cfUtils.Logic.Base.Structures;
    using codingfreaks.cfUtils.Logic.Base.Utilities;

    internal class Program
    {
        #region methods

        /// <summary>
        /// Is used 
        /// </summary>
        /// <returns></returns>
        private static List<CommandlineArgumentInfo> GetArguments()
        {
            return new List<CommandlineArgumentInfo>
            {
                new CommandlineArgumentInfo
                {
                    Abbreviation = "a",
                    ArgumentName = "address",
                    Description = "The address of the target.",
                    IsMandatory = true,
                    OrderPosition = 1,
                    SampleValue = "myserver"
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "p",
                    ArgumentName = "ports",
                    Description = "The comma-separated list of ports to ping for. If you just want to ping one port, use only one value.",
                    IsMandatory = true,
                    OrderPosition = 2,
                    IsNumeric = true,
                    IsCommaSeparated = true,
                    SampleValue = "73636, 80"
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "u",
                    ArgumentName = "udp",
                    Description = "If set, a UDP ping will be performed.",                    
                    IsFlag = true
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "tim",
                    ArgumentName = "timeout",
                    Description = "Defines the timeout in seconds the app will wait for requests to return.",
                    IsNumeric = true,
                    SampleValue = "1",
                    DefaultValue = "1"
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "r",
                    ArgumentName = "repeats",
                    Description = "Defines the amount of requests which will be sent to the target (default is 4).",
                    IsNumeric = true,
                    SampleValue = "1",
                    DefaultValue = "4"
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "l",
                    ArgumentName = "logo",
                    Description = "If set, the app-header will be printed out.",
                    IsFlag = true
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "res",
                    ArgumentName = "resolve",
                    Description = "If set, the app will resolve the DNS name of the target.",
                    IsFlag = true
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "t",
                    ArgumentName = "endless",
                    Description = "If set, the app will run infinitely. (see -as option).",
                    IsFlag = true
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "as",
                    ArgumentName = "autostop",
                    Description = "If set, the app will stop working when it gets one OPEN-result.",
                    IsFlag = true
                },
                new CommandlineArgumentInfo
                {
                    Abbreviation = "w",
                    ArgumentName = "waittime",
                    Description = "",
                    IsNumeric = true,
                    SampleValue = "3000",
                    DefaultValue = "500"
                }
            };
        }

        private static void Main(string[] args)
        {
            if (string.Join(" ", args).Contains("logo"))
            {
                ConsoleUtil.ShowAppHeader(Assembly.GetExecutingAssembly());
            }
            var appInfo = new ApplicationInfo
            {
                AssemblyInfo = Assembly.GetExecutingAssembly(),
                CommandlineArgumentInfos = GetArguments(),
                ParameterDelimiter = '=',
                ParameterPraefix = '-'
            };
            if (!AppUtil.AreCommandArgumentsValid(args, appInfo))
            {
                ConsoleUtil.PrintArgumentError(appInfo, string.Join("\n", AppUtil.CheckCommandArguments(args, appInfo).AsEnumerable()));
            }
            else
            {
                var list = AppUtil.MapCommandArguments(args, appInfo);
                var result = 0;
                string givenValue = null;
                var ports = new List<int>();
                var portValue = string.Empty;
                var timeout = 0;
                var repeats = 0;
                var autoStop = false;
                var useUdp = false;
                try
                {
                    givenValue = list.First(a => a.Abbreviation == "a").GivenValue;
                    portValue = list.First(a => a.Abbreviation == "p").GivenValue;
                    if (portValue.Contains(","))
                    {
                        portValue.Split(',').ToList().ForEach(p => ports.Add(int.Parse(p.Trim())));
                    }
                    else
                    {
                        ports.Add(int.Parse(portValue));
                    }                    
                    timeout = int.Parse(list.First(a => a.Abbreviation == "tim").ResolvedValue);
                    repeats = int.Parse(list.First(a => a.Abbreviation == "r").ResolvedValue);
                    autoStop = list.SingleOrDefault(a => a.Abbreviation == "as") != null;
                    useUdp = list.SingleOrDefault(a => a.Abbreviation == "u") != null;
                    if (list.SingleOrDefault(a => a.Abbreviation == "t") != null)
                    {
                        repeats = int.MaxValue;
                    }
                    var commandlineArgumentInfo = list.SingleOrDefault(a => a.Abbreviation == "w");
                    if (commandlineArgumentInfo != null)
                    {
                        int.TryParse(commandlineArgumentInfo.ResolvedValue, out result);
                    }
                }
                catch (Exception ex)
                {                  
                    ConsoleUtil.PrintColoredLine($"Error during parsing input parameters: {ex.Message}", ConsoleColor.Red);
                    return;
                }
                if (string.IsNullOrEmpty(givenValue) || ports.Any(p => p <= 0))
                {
                    return;
                }
                Console.WriteLine("Starting pinging host {0} on {3} port(s) {1} {2} times:", givenValue, portValue, repeats == int.MaxValue ? "infinite" : repeats.ToString(CultureInfo.InvariantCulture), useUdp ? "UDP" : "TCP");
                var reachablePorts = 0;
                var closedPorts = 0;
                var hostIp = "-";                
                if (list.FirstOrDefault(a => a.Abbreviation == "res") != null)
                {
                    // we have to perform address resolution                    
                    Dns.BeginGetHostEntry(
                        givenValue,
                        ar =>
                        {
                            IPHostEntry local0 = null;
                            try
                            {
                                local0 = Dns.EndGetHostEntry(ar);
                            }
                            catch 
                            {                                
                            }                            
                            if (local0 == null || !local0.AddressList.Any())
                            {
                                return;
                            }
                            hostIp = local0.AddressList[0].ToString();
                        },
                        null);
                }
                // start the process
                var currentPack = 0;
                for (var i = 0; i < repeats; ++i)
                {                    
                    var portOpen = true;
                    ports.ForEach(
                        port =>
                        {
                            try
                            {
                                portOpen &= NetworkUtil.IsPortOpened(givenValue, port, timeout, useUdp);
                                reachablePorts += portOpen ? 1 : 0;
                                closedPorts += portOpen ? 0 : 1;
                                Console.Write("#{0,4} -> Pinging host {1} (IP:{2}) on {5} port {3} with timeout {4}: ", ++currentPack, givenValue, hostIp, port, timeout, useUdp ? "UDP" : "TCP");
                                ConsoleUtil.PrintColoredLine(portOpen ? "OPEN" : "CLOSED", portOpen ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
                            }
                            catch (Exception ex)
                            {
                                ConsoleUtil.PrintColoredLine(string.Format("#{0,4} Error pinging host {1}: {2}", ++currentPack, givenValue, ex.Message), ConsoleColor.Red);
                            }                         
                        });
                    if (!autoStop || !portOpen)
                    {
                        if (result > 0)
                        {
                            Thread.Sleep(result);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                Console.WriteLine("Finished pinging host {0} (IP:{1}). {2} pings sent ({3} OPEN, {4} CLOSED)", givenValue, hostIp, currentPack, reachablePorts, closedPorts);
            }
        }

        #endregion
    }
}