//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.IO.Compression;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class ZipArchive : Task
    {
        public string SourceDirectoryName { get; set; }
        public string DestinationArchiveFileName { get; set; }
        public string RenameBaseDirectoryTo { get; set; }

        public override bool Execute ()
        {
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