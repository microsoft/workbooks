//
// GeneratePublicationManifest.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;

using Xamarin.Versioning;

namespace Xamarin.XamPub.MSBuild
{
	public sealed class GeneratePublicationManifest : Task
	{
		[Required]
		public ITaskItem OutputFile { get; private set; }

		public string UpdateInfoFile { get; set; }

		public string [] UpdaterReleaseNotes { get; set; }

		[Required]
		public ITaskItem [] FilesToInclude { get; set; }

		[Required]
		public string RelativePublishBaseUrl { get; set; }

		public string ReleaseName { get; set; }

		public override bool Execute ()
		{
			var publicationItems = new List<PublicationItem> ();

			foreach (var item in FilesToInclude) {
				if (new FileInfo (item.ItemSpec).FullName ==
					new FileInfo (OutputFile.ItemSpec).FullName)
					continue;

				var publicationItem = ProcessItem (item.ItemSpec, item.GetMetadata ("Evergreen"));
				if (publicationItem == null)
					return false;

				publicationItems.Add (publicationItem);
			}

			using (var writer = new StreamWriter (OutputFile.ItemSpec))
				new JsonSerializer {
					Formatting = Formatting.Indented,
					NullValueHandling = NullValueHandling.Ignore,
					DefaultValueHandling = DefaultValueHandling.Ignore
				}.Serialize (writer, new Publication {
					Info = new PublicationInfo {
						Name = ReleaseName
					},
					Release = publicationItems.ToArray ()
				});

			return true;
		}

		PublicationItem ProcessItem (string path, string evergreenName)
		{
			PublicationItem item;
			try {
				item = PublicationItem.CreateFromFile (path);
			} catch (Exception e) {
				Log.LogError ($"error creating ingestion item for '{path}': {e.Message}");
				return null;
			}

			var fileName = Path.GetFileName (path);

			item.IngestionUri = new Uri (fileName, UriKind.Relative);
			item.RelativePublishUrl = new Uri (
				$"{RelativePublishBaseUrl}/{fileName}",
				UriKind.Relative);

			if (!string.IsNullOrEmpty (evergreenName))
				item.RelativePublishEvergreenUrl = new Uri (
					$"{RelativePublishBaseUrl}/{evergreenName}",
					UriKind.Relative);

			return ProcessItem (item);
		}

		static readonly Regex updaterFileRegex = new Regex (
			@"^(?<name>[\w-_]+)-(?<version>\d+.*)(?<extension>\.(msi|pkg|dmg))$",
			RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

		static readonly Regex pdbArchiveRegex = new Regex (
			@"\-PDB\-.+\.zip$",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		PublicationItem ProcessItem (PublicationItem item)
		{
			var relativePath = item.RelativePublishUrl.ToString ();
			var relativePathFileName = Path.GetFileName (relativePath);

			if (pdbArchiveRegex.IsMatch (relativePathFileName)) {
				item.RelativePublishUrl = null;
				return item;
			}

			var updaterItem = updaterFileRegex.Match (relativePathFileName);
			if (updaterItem == null || !updaterItem.Success)
				return item;

			if (string.IsNullOrEmpty (ReleaseName))
				ReleaseName = $"{updaterItem.Groups ["name"]}-{updaterItem.Groups ["version"]}";

			if (UpdateInfoFile != null)
				item.UpdaterProduct = UpdaterProduct.FromUpdateInfo (
					File.ReadAllText (UpdateInfoFile));
			else
				item.UpdaterProduct = new UpdaterProduct ();

			item.UpdaterProduct.Size = item.Size;
			item.UpdaterProduct.Version = updaterItem.Groups ["version"].Value;

			if (UpdaterReleaseNotes != null)
				item.UpdaterProduct.ReleaseNotes = string
					.Join ("\n", UpdaterReleaseNotes)
					.Trim ();

			if (ReleaseVersion.TryParse (item.UpdaterProduct.Version, out var version)) {
				if (version.CandidateLevel == ReleaseCandidateLevel.Stable)
					item.RelativePublishEvergreenUrl = new Uri (
						Path.GetDirectoryName (relativePath) + "/" +
							updaterItem.Groups ["name"].Value +
							updaterItem.Groups ["extension"].Value,
						UriKind.Relative);

				switch (version.CandidateLevel) {
				case ReleaseCandidateLevel.Alpha:
					item.UpdaterProduct.IsAlpha = true;
					break;
				case ReleaseCandidateLevel.Beta:
				case ReleaseCandidateLevel.StableCandidate:
					item.UpdaterProduct.IsAlpha = true;
					item.UpdaterProduct.IsBeta = true;
					break;
				case ReleaseCandidateLevel.Stable:
					item.UpdaterProduct.IsAlpha = true;
					item.UpdaterProduct.IsBeta = true;
					item.UpdaterProduct.IsStable = true;
					break;
				}
			}

			return item;
		}
	}
}