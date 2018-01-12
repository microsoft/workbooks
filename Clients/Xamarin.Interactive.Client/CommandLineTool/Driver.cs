//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.CommandLineTool
{
    public sealed class Driver
    {
        public string [] ClientLaunchUris { get; set; }
        public bool Verbose { get; set; }

        public void LogErrorVerbose (string message)
        {
            if (Verbose)
                Console.Error.WriteLine (message);
        }

        public void LogVerbose (string message)
        {
            if (Verbose)
                Console.WriteLine (message);
        }

        public int Run ()
        {
            var isMac = Environment.OSVersion.Platform == PlatformID.Unix;
            string workbooksApp;

            var driverLocation = Assembly.GetExecutingAssembly ().Location;
            if (isMac) {
                workbooksApp = new FilePath (Path.GetDirectoryName (driverLocation)).Combine (
                    "..",
                    "..").FullPath;
            } else {
                workbooksApp = Assembly.GetExecutingAssembly ().Location;
            }

            var launchUris = new List<ClientSessionUri> ();

            foreach (var uriString in ClientLaunchUris) {
                ClientSessionUri uri = null;

                if (File.Exists (uriString) || Directory.Exists (uriString)) {
                    try {
                        var fileUri = new Uri (
                            Path.Combine (Environment.CurrentDirectory, uriString));
                        uri = new ClientSessionUri (fileUri);
                    } catch {
                    }
                }

                if (uri == null && !ClientSessionUri.TryParse (uriString, out uri)) {
                    AgentType agentType;

                    if (String.Equals (uriString, "mac", StringComparison.OrdinalIgnoreCase))
                        agentType = AgentType.MacNet45;
                    else if (!Enum.TryParse (uriString, true, out agentType))
                        throw new Exception ($"Invalid URI or platform '{uriString}'");

                    uri = new ClientSessionUri (agentType, ClientSessionKind.Workbook);
                }

                launchUris.Add (uri);
            }

            if (launchUris.Count == 0)
                launchUris.Add (new ClientSessionUri (AgentType.Console, ClientSessionKind.Workbook));

            var workingDirectory = Environment.CurrentDirectory;

            var arguments = new List<string> ();
            foreach (var launchUri in launchUris) {
                var uri = launchUri;
                if (uri.WorkbookPath == null)
                    uri = uri.WithWorkingDirectory (workingDirectory);

                arguments.Add ($"\"{uri}\"");
            }

            if (isMac) {
                arguments.Insert (0, $"\"{workbooksApp}\"");
                arguments.Insert (0, "-a");
                Exec ("open", arguments);
            } else
                Exec (workbooksApp, arguments);

            return 0;
        }

        void Exec (string filename, List<string> arguments)
        {
            var joinedArguments = String.Join (" ", arguments);
            LogVerbose ($"{filename} {joinedArguments}");
            Process.Start (filename, joinedArguments);
        }
    }
}