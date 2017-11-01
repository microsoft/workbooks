//
// InteractiveInstallation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

#if !NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Win32;

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive
{
	class InteractiveInstallation
	{
		public static InteractiveInstallation Default { get; private set; }

		public static void InitializeDefault (bool isMac, string buildPath)
		{
			if (Default != null)
				throw new InvalidOperationException ("InitializeDefault has already been called");

			Default = new InteractiveInstallation (isMac, buildPath, null);
		}

		const string MacFrameworkInstallPath = "Library/Frameworks/Xamarin.Interactive.framework/Versions/Current";

		readonly Dictionary<AgentType, List<string>> agentAssemblyPaths
			= new Dictionary<AgentType, List<string>> ();
		readonly Dictionary<AgentType, List<string>> formsAgentAssemblyPaths
			= new Dictionary<AgentType, List<string>> ();
		List<string> clientAppPaths;
		List<string> netStandardRefAssemblyPaths;

		readonly string workbooksClientInstallPath;
		readonly string inspectorClientInstallPath;
		readonly string agentsInstallPath;
		readonly string toolsInstallPath;

		public string BuildPath { get; }
		public string WorkbookAppsInstallPath { get; }

		public bool IsMac { get; }

		public InteractiveInstallation (bool isMac, string buildPath, string installRootPath)
		{
			// May come in null if initialized by an installed app
			BuildPath = buildPath ?? String.Empty;

			if (isMac) {
				IsMac = true;
				if (String.IsNullOrEmpty (installRootPath))
					installRootPath = "/";

				workbooksClientInstallPath = Path.Combine (
					installRootPath,
					"Applications");

				inspectorClientInstallPath = Path.Combine (
					installRootPath,
					MacFrameworkInstallPath,
					"InspectorClient");
				
				agentsInstallPath = Path.Combine (
					installRootPath,
					MacFrameworkInstallPath);

				WorkbookAppsInstallPath = agentsInstallPath;

				toolsInstallPath = Path.Combine (
					installRootPath,
					MacFrameworkInstallPath,
					"Tools");

				return;
			}

			// On Windows, client and agent assemblies are installed elsewhere
			using (var clientExeKey = RegistryKey.OpenBaseKey (RegistryHive.LocalMachine, RegistryView.Registry32)
				.OpenSubKey (@"Software\Xamarin\Inspector\")) {
				if (clientExeKey == null)
					return;

				var clientExePath = clientExeKey.GetValue ("location") as string;
				if (String.IsNullOrEmpty (clientExePath))
					return;

				inspectorClientInstallPath = workbooksClientInstallPath =
					Path.GetDirectoryName (clientExePath);

				agentsInstallPath = WorkbookAppsInstallPath =
					Path.GetDirectoryName (Path.GetDirectoryName (clientExePath));

				// agentsInstallPath is the root of our installation in this case--it
				// hasn't had `Agents` combined onto it yet, so this is fine.
				toolsInstallPath = Path.Combine (agentsInstallPath, "Tools");
			}
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

		internal IReadOnlyList<string> LocateSimCheckerExecutables ()
		{
			if (simCheckerExecutablePaths != null)
				return simCheckerExecutablePaths;

			var searchPaths = new List<string> ();
			if (toolsInstallPath != null)
				searchPaths.Add (toolsInstallPath);
			if (BuildPath != null)
				searchPaths.Add (Path.Combine (
					BuildPath, "Clients", "Xamarin.Interactive.Client.Mac.SimChecker"));

			simCheckerExecutablePaths = LocateFiles (searchPaths, "Xamarin.Interactive.Mac.SimChecker.exe").ToList ();
			return simCheckerExecutablePaths;
		}

		public string LocateNetStandardRefAssembly ()
			=> LocateNetStandardRefAssemblies ().FirstOrDefault ();

		internal IReadOnlyList<string> LocateNetStandardRefAssemblies ()
		{
			if (netStandardRefAssemblyPaths != null)
				return netStandardRefAssemblyPaths;

			var searchPaths = new List<string> ();
			if (agentsInstallPath != null)
				searchPaths.Add (agentsInstallPath);
			if (BuildPath != null)
				searchPaths.Add (Path.Combine (
					BuildPath, "Agents", "Xamarin.Interactive.NetStandard"));

			netStandardRefAssemblyPaths = LocateFiles (searchPaths, "ref-Xamarin.Interactive.dll").ToList ();

			return netStandardRefAssemblyPaths;
		}

		public string LocateAgentAssembly (AgentType agentType)
			=> LocateAgentAssemblies (agentType).FirstOrDefault ();

		internal IReadOnlyList<string> LocateAgentAssemblies (AgentType agentType)
		{
			List<string> paths;
			if (agentAssemblyPaths.TryGetValue (agentType, out paths))
				return paths;

			var searchPaths = new List<string> ();
			if (agentsInstallPath != null)
				searchPaths.Add (Path.Combine (agentsInstallPath, "Agents"));

			string assemblyName = null;
			switch (agentType) {
			case AgentType.iOS:
				assemblyName = "Xamarin.Interactive.iOS.dll";
				searchPaths.Add (Path.Combine (
					BuildPath, "Agents", "Xamarin.Interactive.iOS", "bin"));
				break;
			case AgentType.Android:
				assemblyName = "Xamarin.Interactive.Android.dll";
				searchPaths.Add (Path.Combine (
					BuildPath, "Agents", "Xamarin.Interactive.Android.ActivityTrackerShim", "bin"));
				break;
			case AgentType.MacNet45:
				assemblyName = "Xamarin.Interactive.Mac.Desktop.dll";
				searchPaths.Add (Path.Combine (
					BuildPath, "Agents", "Xamarin.Interactive.Mac.Desktop", "bin"));
				break;
			case AgentType.MacMobile:
				assemblyName = "Xamarin.Interactive.Mac.Mobile.dll";
				searchPaths.Add (Path.Combine (
					BuildPath, "Agents", "Xamarin.Interactive.Mac.Mobile", "bin"));
				break;
			case AgentType.WPF:
				assemblyName = "Xamarin.Interactive.Wpf.dll";
				if (inspectorClientInstallPath != null)
					searchPaths.Add (inspectorClientInstallPath);
				searchPaths.Add (Path.Combine (
					BuildPath, "Agents", "Xamarin.Interactive.Wpf", "bin"));
				break;
			case AgentType.Console:
				assemblyName = "Xamarin.Interactive.Console.exe";
				searchPaths.Add (Path.Combine (
					BuildPath, "Agents", "Xamarin.Interactive.Console", "bin"));
				break;
			case AgentType.DotNetCore:
				assemblyName = "Xamarin.Interactive.DotNetCore.dll";
				searchPaths.Add (Path.Combine (
					BuildPath, "Agents", "Xamarin.Interactive.DotNetCore", "bin"));
				break;
			default:
				throw new ArgumentException ($"invalid AgentType: {agentType}", nameof (agentType));
			}

			agentAssemblyPaths.Add (agentType, paths = LocateFiles (searchPaths, assemblyName).ToList ());
			return paths;
		}

		public string LocateClientApplication (
			ClientSessionKind clientSessionKind = ClientSessionKind.LiveInspection)
			=> LocateClientApplications (clientSessionKind).FirstOrDefault ();

		internal IReadOnlyList<string> LocateClientApplications (ClientSessionKind clientSessionKind)
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

#endif