// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class FindDirectories : Task
    {
        [Required]
        public string BasePath { get; set; }

        [Required]
        public string [] IncludeNames { get; set; }

        public string [] ExcludeNames { get; set; }

        [Output]
        public string [] Directories { get; set; }

        public override bool Execute ()
        {
            var paths = new List<string> ();
            var includeNames = new HashSet<string> (IncludeNames);
            var excludeNames = new HashSet<string> (ExcludeNames ?? Array.Empty<string> ());

            void Walk (DirectoryInfo directory)
            {
                foreach (var child in directory.EnumerateDirectories ()) {
                    if (includeNames.Contains (child.Name))
                        paths.Add (child.FullName);
                    else if (!excludeNames.Contains (child.Name))
                        Walk (child);
                }
            }

            Walk (new DirectoryInfo (BasePath));

            Directories = paths.ToArray ();

            return true;
        }
    }
}