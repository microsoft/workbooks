//
// WindowsClientApp.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.Win32;

using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.IO.Windows;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Client
{
	sealed class WindowsClientApp : ClientApp
	{
		sealed class WindowsHostEnvironment : HostEnvironment
		{
			public override HostOS OSName { get; } = HostOS.Windows;

			public override string OSVersionString { get; } = Environment.OSVersion.VersionString;

			public override Version OSVersion { get; } = Environment.OSVersion.Version;

			public override int? ProcessorCount => Environment.ProcessorCount;

			public override ulong? PhysicalMemory => MEMORYSTATUSEX.Get ()?.ullTotalPhys;

			public WindowsHostEnvironment () : base (LoadSoftwareEnvironmentsAsync)
			{
			}

			static async Task<IReadOnlyList<ISoftwareEnvironment>> LoadSoftwareEnvironmentsAsync ()
			{
				var environments = new List<ISoftwareEnvironment> {
					new SystemSoftwareEnvironment {
						new XamarinComponent ("iOS"),
						new XamarinComponent ("Android"),
						new XamarinComponent ("Simulator")
					}
				};

				environments.AddRange (await VisualStudioEnvironment.GetInstallations ());

				return environments;
			}

			[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
			sealed class MEMORYSTATUSEX
			{
				public uint dwLength;
				public uint dwMemoryLoad;
				public ulong ullTotalPhys;
				public ulong ullAvailPhys;
				public ulong ullTotalPageFile;
				public ulong ullAvailPageFile;
				public ulong ullTotalVirtual;
				public ulong ullAvailVirtual;
				public ulong ullAvailExtendedVirtual;

				MEMORYSTATUSEX ()
					=> dwLength = (uint)Marshal.SizeOf (typeof (MEMORYSTATUSEX));

				[DllImport ("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
				[return: MarshalAs (UnmanagedType.Bool)]
				static extern bool GlobalMemoryStatusEx ([In, Out] MEMORYSTATUSEX lpBuffer);

				public static MEMORYSTATUSEX Get ()
				{
					try {
						var status = new MEMORYSTATUSEX ();
						if (GlobalMemoryStatusEx (status))
							return status;
					} catch {
					}

					return null;
				}
			}
		}

		protected override ClientAppPaths CreateClientAppPaths ()
		{
			FilePath appData = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			FilePath localAppData = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);

			return new ClientAppPaths (
				localAppData.Combine ("Xamarin", "Inspector", "logs"),
				appData.Combine ("Xamrin", "Inspector"),
				localAppData.Combine ("Xamarin", "Inspector", "Cache"));
		}

		protected override HostEnvironment CreateHostEnvironment ()
			=> new WindowsHostEnvironment ();

		protected override IPreferenceStore CreatePreferenceStore ()
			=> new RegistryPreferenceStore (
				RegistryHive.CurrentUser,
				RegistryView.Registry32,
				@"Software\Xamarin\Inspector\Preferences");

		protected override IFileSystem CreateFileSystem ()
			=> new WindowsFileSystem ();

		protected override ClientWebServer CreateClientWebServer ()
			=> new ClientWebServer (
				new FilePath (Assembly.GetEntryAssembly ().Location)
					.ParentDirectory
					.Combine ("ClientApp"));

		protected override UpdaterService CreateUpdaterService ()
			=> new UpdaterService ("win", "201185fb-fefe-4996-bdfe-4b6ac311a73b");
	}
}