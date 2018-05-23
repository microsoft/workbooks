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

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive
{
    class InteractiveInstallation
    {
        public static InteractiveInstallation Default { get; private set; }

        public static void InitializeDefault (
            bool isMac,
            string buildPath,
            InteractiveInstallationPaths installationPaths = null)
        {
            if (Default != null)
                throw new InvalidOperationException ("InitializeDefault has already been called");

            Default = new InteractiveInstallation (isMac, buildPath, installationPaths);
        }

        readonly Dictionary<AgentType, List<string>> agentAssemblyPaths
            = new Dictionary<AgentType, List<string>> ();
        readonly Dictionary<AgentType, List<string>> formsAgentAssemblyPaths
            = new Dictionary<AgentType, List<string>> ();
        List<string> clientAppPaths;

        readonly string workbooksClientInstallPath;
        readonly string inspectorClientInstallPath;
        readonly string agentsInstallPath;
        readonly string toolsInstallPath;

        public string BuildPath { get; }

        public bool IsMac { get; }

        InteractiveInstallation (
            bool isMac,
            string buildPath,
            InteractiveInstallationPaths installationPaths)
        {
            IsMac = isMac;

            // May come in null if initialized by an installed app
            BuildPath = buildPath ?? String.Empty;

            workbooksClientInstallPath = installationPaths?.WorkbooksClientInstallPath;
            inspectorClientInstallPath = installationPaths?.InspectorClientInstallPath;
            agentsInstallPath = installationPaths?.AgentsInstallPath;
            toolsInstallPath = installationPaths?.ToolsInstallPath;
        }

        public string LocateFormsAssembly (AgentType agentType)
        {
            List<string> paths;
            if (formsAgentAssemblyPaths.TryGetValue (agentType, out paths))
                return paths.First ();

            var searchPaths = new List<string> ();
            if (agentsInstallPath != null)
                searchPaths.Add (Path.Combine (agentsInstallPath, "Agents", "Forms"));

            string assemblyName = null;
            switch (agentType) {
            case AgentType.iOS:
                assemblyName = "Xamarin.Interactive.Forms.iOS.dll";
                searchPaths.Add (Path.Combine (
                    BuildPath, "Agents", "Xamarin.Interactive.Forms.iOS", "bin"));
                break;
            case AgentType.Android:
                assemblyName = "Xamarin.Interactive.Forms.Android.dll";
                searchPaths.Add (Path.Combine (
                    BuildPath, "Agents", "Xamarin.Interactive.Forms.Android", "bin"));
                break;
            default:
                return null;
            }

            formsAgentAssemblyPaths.Add (agentType, paths = LocateFiles (searchPaths, assemblyName).ToList ());
            return paths.First ();
        }

        List<string> simCheckerExecutablePaths;

        public string LocateSimChecker () => LocateSimCheckerExecutables ().FirstOrDefault ();

        IReadOnlyList<string> LocateSimCheckerExecutables ()
        {
            if (simCheckerExecutablePaths != null)
                return simCheckerExecutablePaths;

            var searchPaths = new List<string> ();
            if (toolsInstallPath != null)
                searchPaths.Add (toolsInstallPath);
            if (BuildPath != null)
                searchPaths.Add (Path.Combine (
                    BuildPath, "Clients", "Xamarin.Interactive.Client.Mac.SimChecker"));

            simCheckerExecutablePaths = LocateFiles (searchPaths, "Xamarin.Interactive.Client.Mac.SimChecker.exe").ToList ();
            return simCheckerExecutablePaths;
        }

        public string LocateClientApplication (
            ClientSessionKind clientSessionKind = ClientSessionKind.LiveInspection)
            => LocateClientApplications (clientSessionKind).FirstOrDefault ();

        IReadOnlyList<string> LocateClientApplications (ClientSessionKind clientSessionKind)
        {
            if (clientAppPaths != null)
                return clientAppPaths;

            var searchPaths = new List<string> ();

            var clientInstallPath = clientSessionKind == ClientSessionKind.LiveInspection
                ? inspectorClientInstallPath
                : workbooksClientInstallPath;

            string appFileName;

            if (IsMac) {
                appFileName = clientSessionKind == ClientSessionKind.LiveInspection
                    ? "Xamarin Inspector.app"
                    : "Xamarin Workbooks.app";
                searchPaths.Add (Path.Combine (
                    BuildPath, "Clients", "Xamarin.Interactive.Client.Mac", "bin"));
            } else {
                appFileName = clientSessionKind == ClientSessionKind.LiveInspection
                    ? "Xamarin Inspector.exe"
                    : "Xamarin Workbooks.exe";
                searchPaths.Add (Path.Combine (
                    BuildPath, "Clients", "Xamarin.Interactive.Client.Windows", "bin"));
            }

            var foundPaths = LocateFiles (searchPaths, appFileName).ToList ();
            if (clientInstallPath != null) {
                var installedPath = Path.Combine (clientInstallPath, appFileName);
                if (File.Exists (installedPath) || Directory.Exists (installedPath))
                    foundPaths.Add (installedPath);
                // Resort in descending mtime order
                foundPaths.Sort ((x, y) =>
                    File.GetLastWriteTimeUtc (GetPathForOrdering (y)).CompareTo (
                        File.GetLastWriteTimeUtc (GetPathForOrdering (x))));
            }

            return clientAppPaths = foundPaths;
        }

        internal static IEnumerable<string> LocateFiles (IEnumerable<string> searchPaths, string searchPattern) =>
            from searchPath in searchPaths
            from path in EnumerateFiles (searchPath, searchPattern)
            orderby File.GetLastWriteTimeUtc (GetPathForOrdering (path)) descending
            select path;

        /// <summary>
        /// When encountering a .app bundle, return the bundle executable file so
        /// time stamp comparisons are more accurate.
        /// </summary>
        static string GetPathForOrdering (string path)
        {
            if (Path.GetExtension (path) != ".app")
                return path;

            var binaryName = Path.GetFileNameWithoutExtension (path);
            var macAppBinary = Path.Combine (path, "Contents", "MacOS", binaryName);
            if (File.Exists (macAppBinary))
                return macAppBinary;

            var iOSAppBinary = Path.Combine (path, binaryName);
            if (File.Exists (iOSAppBinary))
                return iOSAppBinary;

            return path;
        }

        static IEnumerable<string> EnumerateFiles (string path, string searchPattern, bool recursive = true)
        {
            if (!Directory.Exists (path))
                yield break;

            foreach (var child in Directory.EnumerateFileSystemEntries (
                path, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                yield return child;
        }
    }
}
