//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Xamarin.Interactive.Client.CommandLineTool
{
    static class Entry
    {
        public static int Run (string [] args, out bool shouldExit)
        {
            if (args.Length > 0 && args [0] == "test") {
                TestDriver.Initialize (args.Skip (1).ToArray ());
                shouldExit = false;
                return 0;
            }

            shouldExit = true;

            var driver = new Driver ();
            var showVersion = false;
            var showHelp = false;

            var positionalArguments = new List<string> ();

            for (int i = 0; i < args.Length; i++) {
                var a = args [i];
                if (String.IsNullOrEmpty (a))
                    continue;

                switch (a.ToLowerInvariant ()) {
                case "/version":
                case "-version":
                case "--version":
                    showVersion = true;
                    break;
                case "/h":
                case "/?":
                case "-h":
                case "-?":
                case "/help":
                case "-help":
                case "--help":
                    showHelp = true;
                    break;
                case "/v":
                case "-v":
                case "/verbose":
                case "-verbose":
                case "--verbose":
                    driver.Verbose = true;
                    break;
                default:
                    if (!File.Exists (a) && !Directory.Exists (a) && (a [0] == '-' || a [0] == '/')) {
                        Console.Error.WriteLine ($"error: unrecognized option: {a}");
                        return 1;
                    }

                    positionalArguments.Add (a);
                    break;
                }
            }

            if (showHelp) {
                Console.Error.WriteLine ("Usage: workbook [OPTIONS] [PLATFORM | FILE | URI]");
                Console.Error.WriteLine ();
                Console.Error.WriteLine ("Options:");
                Console.Error.WriteLine ("  -version           show version information");
                Console.Error.WriteLine ("  -help, -h          show this help");
                Console.Error.WriteLine ("  -verbose, -v       show verbose logging");
                Console.Error.WriteLine ();
                Console.Error.WriteLine ("PLATFORM may be one of:");
                foreach (AgentType agentType in Enum.GetValues (typeof (AgentType))) {
                    switch (agentType) {
                    case AgentType.Unknown:
                    case AgentType.Test:
                        break;
                    default:
                        var platformName = Enum
                            .GetName (typeof (AgentType), agentType)
                            .ToLowerInvariant ();
                            Console.Error.WriteLine ("  {0}", platformName);
                        break;
                    }
                }
                return 2;
            }

            if (showVersion) {
                Console.Write ("Xamarin Interactive version ");
                Console.WriteLine ($"{BuildInfo.Version} ({BuildInfo.Hash}) {BuildInfo.Date}");
                Console.WriteLine (BuildInfo.Copyright);
                return 3;
            }

            driver.ClientLaunchUris = positionalArguments.ToArray ();

            try {
                return driver.Run ();
            } catch (Exception e) {
                if (driver.Verbose)
                    Console.Error.WriteLine (e);
                else
                    Console.Error.WriteLine ("error: {0}", e.Message);

                return 4;
            }
        }
    }
}