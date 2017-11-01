//
// VisualStudioEnvironment.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Xamarin.ProcessControl;

namespace Xamarin.Interactive.SystemInformation
{
	/// <summary>
	/// Reads information from a side-by-side (2017+) Visual Studio installation
	/// via <c>vswhere.exe</c> from the Visual Studio Installer.
	/// </summary>
	sealed class VisualStudioEnvironment : ISoftwareEnvironment
	{
		static readonly string vsWherePath = Path.Combine (
			Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86),
			"Microsoft Visual Studio",
			"Installer",
			"vswhere.exe");

		public static async Task<IEnumerable<VisualStudioEnvironment>> GetInstallations ()
		{
			if (!File.Exists (vsWherePath))
				return Array.Empty<VisualStudioEnvironment> ();

			var output = new StringWriter ();

			await Exec.RunAsync (
				segment => output.WriteLine (segment.Data),
				vsWherePath,
				"-prerelease",
				"-format", "json").ConfigureAwait (false);

			return JsonConvert
				.DeserializeObject<IEnumerable<VisualStudioInstance>> (
					output.ToString (),
					new JsonSerializerSettings {
						ContractResolver = new CamelCasePropertyNamesContractResolver ()
					})
				.Select (instance => new VisualStudioEnvironment (instance))
				.ToArray ();
		}

		public string Name { get; }

		readonly List<ISoftwareComponent> components = new List<ISoftwareComponent> ();

		VisualStudioEnvironment (VisualStudioInstance vsInstance)
		{
			Name = vsInstance.ChannelId;

			components.Add (vsInstance);

			var xamarinInstallDir = Path.Combine (
				vsInstance.InstallationPath,
				"MSBuild",
				"Xamarin");

			foreach (var product in new [] { "iOS", "Android" })
				components.Add (new XamarinComponent (
					product,
					Path.Combine (xamarinInstallDir, product)));
		}


		public IEnumerator<ISoftwareComponent> GetEnumerator ()
			=> components.GetEnumerator ();

		IEnumerator IEnumerable.GetEnumerator ()
			=> GetEnumerator ();

		sealed class VisualStudioInstance : ISoftwareComponent
		{
			public string InstanceId { get; set; }
			public string InstallDate { get; set; }
			public string InstallationName { get; set; }
			public string InstallationPath { get; set; }
			public string InstallationVersion { get; set; }
			public string DisplayName { get; set; }
			public string Description { get; set; }
			public string EnginePath { get; set; }
			public string ChannelId { get; set; }
			public string ChannelPath { get; set; }
			public string ChannelUri { get; set; }
			public string ReleaseNotes { get; set; }
			public string ThirdPartyNotices { get; set; }
			public bool IsPrerelease { get; set; }

			string ISoftwareComponent.Name => DisplayName;
			string ISoftwareComponent.Version => InstallationName.Split (new [] { '/' }, 2) [1];
			bool ISoftwareComponent.IsInstalled => true;

			public void SerializeExtraProperties (JsonTextWriter writer)
			{
			}
		}
	}
}