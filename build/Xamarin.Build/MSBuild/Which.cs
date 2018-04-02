//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class Which : Task
    {
        [Required]
        public string Program { get; set; }

        public bool FatalIfNotFound { get; set; } = true;

        public string [] PreferPaths { get; set; }

        [Output]
        public string FullPath { get; set; }

        public override bool Execute ()
        {
            var isWindows = RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

            var preferPaths = PreferPaths ?? Array.Empty<string> ();

            var extensions = new List<string> { null, ".exe" };
            if (isWindows)
                extensions.Add (".cmd");

            var searchPaths = preferPaths.Concat (Environment
                .GetEnvironmentVariable ("PATH")
                .Split (isWindows ? ';' : ':'));

            foreach (var programDir in searchPaths) {
                foreach (var extension in extensions) {
                    var programPath = Path.Combine (programDir, Program) + extension;
                    if (File.Exists (programPath)) {
                        FullPath = programPath;
                        Log.LogMessage (MessageImportance.High, "Found '{0}' at '{1}'", Program, FullPath);
                        return true;
                    }
                }
            }

            string flattenedPreferPaths = null;
            var message = "Unable to find '{0}' in ";
            if (preferPaths.Length > 0) {
                message += "specified PreferPaths ({1}) nor PATH";
                flattenedPreferPaths = string.Join (", ", preferPaths.Select (p => $"'{p}'"));
            } else {
                message += "PATH";
            }

            if (FatalIfNotFound) {
                Log.LogError (message, Program, flattenedPreferPaths);
                return false;
            }

            Log.LogWarning (message, Program, flattenedPreferPaths);

            return true;
        }
    }
}