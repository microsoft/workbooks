// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Xamarin.ProcessControl
{
    static class Which
    {
        public static string Exec (string programName, params string [] preferPaths)
        {
            var isWindows = RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

            var extensions = new List<string> (Environment
                .GetEnvironmentVariable ("PATHEXT")
                ?.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string> ());

            if (extensions.Count == 0)
                extensions.Add (".exe");

            extensions.Insert (0, null);

            var searchPaths = preferPaths.Concat (Environment
                .GetEnvironmentVariable ("PATH")
                .Split (isWindows ? ';' : ':'));

            var filesToCheck = searchPaths
                .Where (Directory.Exists)
                .Select (p => new DirectoryInfo (p))
                .SelectMany (p => p.EnumerateFiles ());

            foreach (var file in filesToCheck) {
                foreach (var extension in extensions) {
                    if (string.Equals (file.Name, programName + extension, StringComparison.OrdinalIgnoreCase))
                        return file.FullName;
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

            throw new FileNotFoundException (string.Format (message, programName, flattenedPreferPaths));
        }
    }
}