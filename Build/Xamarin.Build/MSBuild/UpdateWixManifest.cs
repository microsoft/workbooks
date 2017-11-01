//
// UpdateWixManifest.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Xamarin.Interactive;
using Xamarin.Interactive.Core;

namespace Xamarin.MSBuild
{
	public sealed class UpdateWixManifest : Task
	{
		[Required]
		public string SourceDirectory { get; set; }

		[Required]
		public string InputManifestPath { get; set; }

		[Required]
		public string DirectoryVariable { get; set; }

		[Required]
		public string ComponentGroupId { get; set; }

		public string IdPrefix { get; set; } = string.Empty;
		public bool ScanRecursively { get; set; } = false;
		public string [] IncludedExtensions { get; set; } = new [] { "*.dll" };
		public string [] ExcludedExtensions { get; set; } = Array.Empty<string> ();
		public bool FailOnManifestChanges { get; set; } = true;
		public bool UseHashForId { get; set; } = false;

		static readonly Regex AllowedWixIdCharactersRegex = new Regex ("[^a-zA-Z\\._]", RegexOptions.Compiled);
		static readonly XNamespace WiXNamespace = "http://schemas.microsoft.com/wix/2006/wi";
		static readonly SHA1 Hasher = SHA1.Create ();

		public override bool Execute ()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				Log.LogMessage (MessageImportance.High, $"Skipping WiX update, we're not on Windows.");
				return true;
			}

			if (!File.Exists (InputManifestPath))
				throw new ArgumentException ($"Input manifest {InputManifestPath} does not exist.", nameof (InputManifestPath));

			if (!Directory.Exists (SourceDirectory))
				throw new ArgumentException ($"Source directory {SourceDirectory} does not exist.", nameof (SourceDirectory));

			XDocument wixManifest;
			using (var sr = new StreamReader (InputManifestPath))
				wixManifest = XDocument.Load (sr);

			var targetComponentGroup = GetComponentGroup (wixManifest, ComponentGroupId);
			if (targetComponentGroup == null)
				throw new ArgumentException ($"Could not find ComponentGroup with ID {ComponentGroupId}.");

			var hasChanges = false;

			// Handle the easy case first, scanning just the single directory.
			var sourcePath = new FilePath (SourceDirectory);
			var searchOption = ScanRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			var sourceFiles = IncludedExtensions.SelectMany (
				extension => sourcePath.EnumerateFiles ($"*.{extension}", searchOption));
			var excludedHash = new HashSet<string> (ExcludedExtensions, StringComparer.OrdinalIgnoreCase);

			var newFiles = new List<string> ();

			foreach (var file in sourceFiles) {
				var fileName = file.Name;
				var extension = Path.GetExtension (file).TrimStart ('.');
				var relativePath = file.GetRelativePath (sourcePath);

				if (excludedHash.Contains (extension)) {
					Log.LogMessage (
						MessageImportance.Low,
						$"Skipping file {fileName}, its extension matches the exclusion list.");
					continue;
				}

				// Look up by relative path--in the case of non-recursion, relativePath == fileName.
				var hasExisting = HasExistingFileElement (targetComponentGroup, relativePath);
				if (hasExisting) {
					Log.LogMessage (
						MessageImportance.Low,
						$"Skipping file {fileName}, it's already present in the component group.");
					continue;
				}

				// Use the relative path for generating ID hashes--hash IDs are useful for generating
				// a full tree that may have repeated file names.
				var id = UseHashForId ? Hash (file.FullPath) : GenerateIdForFile (IdPrefix, fileName);
				var newComponent = CreateComponentForFile (id, DirectoryVariable, relativePath);

				targetComponentGroup.Add (newComponent);
				hasChanges = true;

				newFiles.Add (fileName);

				Log.LogMessage (
					MessageImportance.Low,
					$"Added file {fileName} (id: {id}) to target component group.");
			}

			if (hasChanges) {
				using (var fs = File.Open (InputManifestPath, FileMode.Create))
					wixManifest.Save (fs);
			}

			if (hasChanges && FailOnManifestChanges) {
				var manifestFileName = Path.GetFileNameWithoutExtension (InputManifestPath);

				Log.LogError (
					$"Manifest {manifestFileName} has changes, and FailOnManifestChanges is true. " +
					$"Please check the manifest output and commit it if it's correct.");

				newFiles.ForEach(newFile => Log.LogError (
					$"New file {newFile} added to manifest {InputManifestPath}."));

				return false;
			}

			return true;
		}

		string Hash (string input)
			=> "File" + Hasher.ComputeHash (Encoding.UTF8.GetBytes (input)).ToHexString ();

		XElement GetComponentGroup (XDocument wixManifest, string componentGroupId)
			=> wixManifest.Descendants (WiXNamespace + "ComponentGroup")
				.SingleOrDefault (cg => cg.Attribute ("Id")?.Value == componentGroupId);

		bool HasExistingFileElement (XElement componentGroup, string filePath)
			=> componentGroup.Descendants (WiXNamespace + "File")
				.Where (wf => wf.Attribute ("Source")?.Value == $"$(var.{DirectoryVariable})\\{filePath}")
				.Any ();

		string GenerateIdForFile (string prefix, string fileName)
		{
			var id = new StringBuilder (prefix);
			if (id.Length > 0)
				id.Append ('.');
			id.Append (AllowedWixIdCharactersRegex.Replace (fileName, "_"));
			return id.ToString ();
		}

		XElement CreateComponentForFile (
			string id,
			string directoryVariable,
			string fileName,
			Guid guid = default (Guid))
		{
			if (guid == Guid.Empty)
				guid = Guid.NewGuid ();

			var newComponent = new XElement (WiXNamespace + "Component");
			newComponent.SetAttributeValue ("Id", id);
			newComponent.SetAttributeValue ("Guid", guid.ToString ("D").ToUpperInvariant ());

			var newFile = new XElement (WiXNamespace + "File");
			newFile.SetAttributeValue ("Id", id);
			newFile.SetAttributeValue ("Source", $"$(var.{DirectoryVariable})\\{fileName}");

			newComponent.Add (newFile);

			return newComponent;
		}
	}
}
