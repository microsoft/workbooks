//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.IO.Compression;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using SIOCZipArchive = System.IO.Compression.ZipArchive;

namespace Xamarin.MSBuild
{
    public sealed class ZipArchive : Task
    {
        public string SourceDirectoryName { get; set; }

        public ITaskItem [] SourceFiles { get; set; }

        [Required]
        public string DestinationArchiveFileName { get; set; }

        public string RenameBaseDirectoryTo { get; set; }

        public override bool Execute ()
        {
            if (SourceFiles == null && SourceDirectoryName == null) {
                Log.LogError (
                    $"{nameof (ZipArchive)}: either {nameof (SourceFiles)} or " +
                    $"{nameof (SourceDirectoryName)} must be provided.");
                return false;
            }

            if (SourceFiles != null) {
                using (var stream = File.OpenWrite (DestinationArchiveFileName))
                using (var archive = new SIOCZipArchive (stream, ZipArchiveMode.Create, true, Encoding.UTF8)) {
                    foreach (var item in SourceFiles) {
                        var fullPath = item.GetMetadata ("FullPath") ?? item.ItemSpec;
                        archive.CreateEntryFromFile (fullPath, item.ItemSpec);
                    }
                }

                return true;
            }

            if (!Directory.Exists (SourceDirectoryName))
                throw new DirectoryNotFoundException (SourceDirectoryName);

            var fullSourceDirectoryName = SourceDirectoryName.TrimEnd (
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            var sourceDirectoryName = fullSourceDirectoryName;

            if (RenameBaseDirectoryTo != null) {
                sourceDirectoryName = Path.Combine (
                    Path.GetDirectoryName (fullSourceDirectoryName),
                    Path.GetFileName (RenameBaseDirectoryTo));

                Directory.Move (fullSourceDirectoryName, sourceDirectoryName);
            }

            Log.LogMessage (
                MessageImportance.High,
                "Creating archive: {0}",
                DestinationArchiveFileName);

            Directory.CreateDirectory (Path.GetDirectoryName (DestinationArchiveFileName));

            ZipFile.CreateFromDirectory (
                sourceDirectoryName,
                DestinationArchiveFileName,
                CompressionLevel.Optimal,
                includeBaseDirectory: true);

            return true;
        }
    }
}