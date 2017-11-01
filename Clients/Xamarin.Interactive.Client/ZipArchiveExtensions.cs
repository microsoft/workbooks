//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive
{
    static class ZipArchiveExtensions
    {
        public static void Extract (
            this ZipArchive archive,
            FilePath outputDirectory,
            bool preserveRootDirectory = true,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            outputDirectory.CreateDirectory ();

            var startsWithSamePrefix = true;
            var samePrefixLength = 0;

            if (!preserveRootDirectory) {
                var checkedFirstPrefix = false;
                string prefix = null;

                foreach (var entry in archive.Entries) {
                    if (!checkedFirstPrefix) {
                        samePrefixLength = entry.FullName.IndexOf ('/') + 1;
                        if (samePrefixLength > 0)
                            prefix = entry.FullName.Substring (0, samePrefixLength);
                        checkedFirstPrefix = true;
                    }

                    if (prefix != null)
                        startsWithSamePrefix &= entry.FullName.StartsWith (
                            prefix,
                            StringComparison.Ordinal);

                    if (!startsWithSamePrefix)
                        break;
                }
            }

            foreach (var entry in archive.Entries) {
                cancellationToken.ThrowIfCancellationRequested ();

                if (String.IsNullOrEmpty (entry.FullName))
                    continue;

                var destEntryName = entry.FullName;
                if (!preserveRootDirectory && startsWithSamePrefix) {
                    destEntryName = entry.FullName.Substring (samePrefixLength);
                    if (destEntryName.Length == 0)
                        continue;
                }

                var fullPath = outputDirectory.Combine (destEntryName);

                if (destEntryName [destEntryName.Length - 1] == '/') {
                        fullPath.CreateDirectory ();
                    continue;
                }

                fullPath.ParentDirectory.CreateDirectory ();

                using (var fsStream = File.Open (fullPath, FileMode.Create, FileAccess.Write))
                using (var entryStream = entry.Open ())
                    entryStream.CopyTo (fsStream);

                File.SetLastWriteTime (fullPath, entry.LastWriteTime.DateTime);
            }
        }

        public static unsafe bool SmellHeader (Stream stream)
        {
            var magicBytes = new byte [4];
            if (stream.Read (magicBytes, 0, magicBytes.Length) == magicBytes.Length) {
                fixed (byte* magicPtr = &magicBytes [0]) {
                    switch (*(int*)magicPtr) {
                    case 0x04034B50:
                    case 0x06054B50:
                    case 0x08074B50:
                        return true;
                    }
                }
            }

            return false;
        }
    }
}