//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using Mono.Options;

namespace Xamarin.Interactive.Client.Web
{
    public class Program
    {
        public static void Main (string [] args)
            => BuildWebHost (args)?.Run ();

        public static IWebHost BuildWebHost (string [] args)
        {
            bool showHelp = false;
            var endpoints = new List<IPEndPoint> ();

            var optionSet = new OptionSet {
                { $"usage: {Assembly.GetEntryAssembly ().GetName ().Name} [OPTIONS]+" },
                { "" },
                { "OPTIONS:" },
                { "" },
                { "h|help", "Show this help.",
                    v => showHelp = true },
                { "l|listen=", "Listen on endpoint (e.g. 0.0.0.0:5000)",
                    v => {
                        var endpoint = v.Split (':');
                        if (endpoint.Length != 2)
                            throw new FormatException ($"Invalid endpoint: {v}");

                        if (!IPAddress.TryParse (endpoint [0], out var ipAddress))
                            throw new FormatException ($"Invalid IP address: {endpoint [0]}");

                        if (!ushort.TryParse (endpoint [1], out ushort port))
                            throw new FormatException ($"Invalid port: {endpoint [1]}");

                        endpoints.Add (new IPEndPoint (ipAddress, port));
                    } }
            };

            try {
                args = optionSet.Parse (args).ToArray ();
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Error.Write ("error: ");
                Console.ResetColor ();
                Console.Error.WriteLine (e.Message);
                Console.Error.WriteLine ();
                showHelp = true;
            }

            if (showHelp) {
                optionSet.WriteOptionDescriptions (Console.Error);
                Environment.Exit (1);
                return null;
            }

            if (endpoints.Count == 0)
                endpoints.Add (new IPEndPoint (IPAddress.Loopback, 5000));

            return WebHost.CreateDefaultBuilder (args)
                .UseStartup<Startup> ()
                .UseKestrel (options => {
                    foreach (var endpoint in endpoints)
                        options.Listen (endpoint);
                })
                .Build ();
        }
    }
}