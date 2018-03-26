//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Xamarin.MSBuild
{
    using static MSBuildProjectFile;

    public sealed class UpdateInvertedDependencies : Task
    {
        public string [] Solutions { get; set; }

        public string [] NpmPackageJson { get; set; }

        public string [] ExcludeProjectNames { get; set; } = Array.Empty<string> ();

        [Required]
        public string OutputFile { get; set; }

        IEnumerable<InvertedDependency> GetNuGetDependencies ()
        {
            if (Solutions == null || Solutions.Length == 0)
                return Array.Empty<InvertedDependency> ();

            var packagesToProjects = new Dictionary<PackageReference, List<MSBuildProjectFile>> ();

            IEnumerable<PackageReference> SelectPackages (MSBuildProjectFile project)
            {
                project.Load ();
                foreach (var packageReference in project.PackageReferences) {
                    if (!packagesToProjects.TryGetValue (
                        packageReference,
                        out var projects)) {
                        projects = new List<MSBuildProjectFile> ();
                        packagesToProjects.Add (packageReference, projects);
                    }
                    projects.Add (project);
                    yield return packageReference;
                }
            }

            return Solutions
                .SelectMany (ParseProjectsInSolution)
                .Where (project => !project.IsSolutionFolder &&
                    !ExcludeProjectNames.Contains (project.Name))
                .OrderBy (project => project.FullPath)
                .Distinct (new FullPathComparer ())
                .SelectMany (SelectPackages)
                .OrderBy (pr => pr)
                .Distinct ()
                .Select (packageReference => new InvertedDependency {
                    Kind = DependencyKind.NuGet,
                    Name = packageReference.Id,
                    Version = packageReference.Version,
                    DependentProjects = packagesToProjects [packageReference]
                        .Select (p => p.Name)
                        .ToArray ()
                });
        }

        IEnumerable<InvertedDependency> GetNpmDependencies ()
        {
            if (NpmPackageJson == null)
                return Array.Empty<InvertedDependency> ();

            return NpmPackageJson.SelectMany (path => JsonConvert
                .DeserializeXNode (File.ReadAllText (path), "_")
                .Root
                .Element ("dependencies")
                .Descendants ()
                .Select (e => new InvertedDependency {
                    Kind = DependencyKind.Npm,
                    Name = e.Name.ToString (),
                    Version = e.Value
            })).OrderBy (e => e.Name).ThenBy (e => e.Version);
        }

        IEnumerable<InvertedDependency> GetGitSubmoduleDependencies ()
        {
            foreach (var line in Exec.Run ("git", "submodule", "status")) {
                var parts = line.Split (
                    new [] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts == null || parts.Length < 2)
                    continue;

                yield return new InvertedDependency {
                    Kind = DependencyKind.GitSubmodule,
                    Name = parts [1],
                    Version = parts [0]
                };
            };
        }

        public override bool Execute ()
        {
            IEnumerable<InvertedDependency> invertedDependencies = Array.Empty<InvertedDependency> ();

            if (File.Exists (OutputFile))
                invertedDependencies = JsonConvert
                    .DeserializeObject<InvertedDependency []> (File.ReadAllText (OutputFile))
                    .Where (ShouldPreserve);

            invertedDependencies = invertedDependencies
                .Concat (GetNuGetDependencies ())
                .Concat (GetNpmDependencies ())
                .Concat (GetGitSubmoduleDependencies ());

            using (var writer = new StreamWriter (OutputFile))
                JsonSerializer.Create (new JsonSerializerSettings {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                }).Serialize (writer, invertedDependencies);

            return true;
        }

        static bool ShouldPreserve (InvertedDependency dependency)
            => dependency != null && dependency.Kind == DependencyKind.GitRepo;

        enum DependencyKind
        {
            None,
            NuGet,
            Npm,
            GitSubmodule,
            GitRepo
        }

        sealed class InvertedDependency
        {
            [JsonConverter (typeof (StringEnumConverter))]
            public DependencyKind Kind { get; set; }

            public string Name { get; set; }
            public string Version { get; set; }
            public string Path { get; set; }
            public string [] DependentProjects { get; set; }
        }
    }
}