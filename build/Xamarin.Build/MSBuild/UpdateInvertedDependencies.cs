//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Xamarin.MSBuild
{
    public sealed class UpdateInvertedDependencies : Task
    {
        public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;

        public string [] NpmPackageJson { get; set; }

        public string [] ExcludeProjectNames { get; set; } = Array.Empty<string> ();

        public string [] ExcludePackageReferences { get; set; } = Array.Empty<string> ();

        public string NuGetTool { get; set; }

        public string NuspecCacheDirectory { get; set; }

        [Required]
        public string JsonOutputFile { get; set; }

        IEnumerable<InvertedDependency> GetNuGetDependencies ()
        {
            var packagesToProjects = new Dictionary<PackageReference, List<Project>> ();
            var packageRoots = new HashSet<string> ();

            IEnumerable<PackageReference> SelectPackages (Project project)
            {
                var packageReferences = project
                    .GetItems ("PackageReference")
                    .Select (pr => new PackageReference (
                        pr.EvaluatedInclude,
                        pr.GetMetadataValue ("Version")));

                var packageRoot = project.GetPropertyValue ("NuGetPackageRoot");
                if (!string.IsNullOrEmpty (packageRoot))
                    packageRoots.Add (packageRoot);

                foreach (var packageReference in packageReferences) {
                    if (ExcludePackageReferences.Any (excluded => string.Equals (
                        excluded, packageReference.Id, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    if (!packagesToProjects.TryGetValue (packageReference, out var projects))
                        packagesToProjects.Add (packageReference, projects = new List<Project> ());

                    projects.Add (project);

                    yield return packageReference;
                }
            }

            string GetNuSpecPath (PackageReference packageReference)
            {
                var packagePath = Path.Combine (
                    packageReference.Id,
                    packageReference.Version,
                    packageReference.Id + ".nuspec");

                var searchPaths = new List<string> (packageRoots);
                if (NuspecCacheDirectory != null)
                    searchPaths.Add (NuspecCacheDirectory);

                return searchPaths
                    .SelectMany (packageRoot => new [] {
                        Path.Combine (packageRoot, packagePath.ToLowerInvariant ()),
                        Path.Combine (packageRoot, packagePath)
                    })
                    .FirstOrDefault (File.Exists);
            }

            void InstallPackage (PackageReference packageReference)
            {
                if (NuGetTool == null || NuspecCacheDirectory == null)
                    return;

                var args = new List<string> {
                    "install",
                    "-NonInteractive",
                    "-PackageSaveMode", "nuspec",
                    "-OutputDirectory", NuspecCacheDirectory,
                    packageReference.Id
                };

                if (!string.IsNullOrEmpty (packageReference.Version)) {
                    args.Add ("-Version");
                    args.Add (packageReference.Version);
                }

                var processStartInfo = new ProcessStartInfo {
                    FileName = NuGetTool,
                    WorkingDirectory = WorkingDirectory,
                    Arguments = Exec.QuoteArguments (args)
                };

                Log.LogMessage (
                    MessageImportance.High,
                    "Installing {0}: {1} {2}",
                    packageReference,
                    processStartInfo.FileName,
                    processStartInfo.Arguments);

                var process = Process.Start (processStartInfo);
                process.WaitForExit ();

                if (process.ExitCode != 0)
                    throw new Exception ("nuget install failed");
            }

            var projectCollection = new ProjectCollection ();

            return Exec.Run (
                new ProcessStartInfo {
                    FileName = "git",
                    WorkingDirectory = WorkingDirectory
                }, "ls-files", "--recurse-submodules", "*.csproj", "*.fsproj", "*.vbproj")
                .Where (projectPath => !ExcludeProjectNames.Contains (Path.GetFileNameWithoutExtension (projectPath)))
                .OrderBy (projectPath => projectPath)
                .Select (projectPath => projectCollection.LoadProject (projectPath))
                .SelectMany (SelectPackages)
                .OrderBy (pr => pr)
                .Distinct ()
                .Select (packageReference => {
                    var invertedDependency = new InvertedDependency {
                        Kind = DependencyKind.NuGet,
                        Name = packageReference.Id,
                        Version = packageReference.Version,
                        DependentProjects = packagesToProjects [packageReference]
                            .Select (p => Path.GetFileNameWithoutExtension (p.FullPath))
                            .ToArray ()
                    };

                    var nuspecPath = GetNuSpecPath (packageReference);
                    if (nuspecPath == null) {
                        InstallPackage (packageReference);
                        nuspecPath = GetNuSpecPath (packageReference);
                    }

                    if (nuspecPath != null) {
                        var nuspec = XDocument.Load (nuspecPath);
                        var ns = nuspec.Root.GetDefaultNamespace ();
                        var metadata = nuspec.Root.Element (ns + "metadata");
                        invertedDependency.ProjectUrl = metadata?.Element (ns + "projectUrl")?.Value;
                        invertedDependency.LicenseUrl = metadata?.Element (ns + "licenseUrl")?.Value;
                    }

                    return invertedDependency;
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

            if (File.Exists (JsonOutputFile))
                invertedDependencies = JsonConvert
                    .DeserializeObject<InvertedDependency []> (File.ReadAllText (JsonOutputFile))
                    .Where (ShouldPreserve);

            invertedDependencies = invertedDependencies
                .Concat (GetGitSubmoduleDependencies ())
                .Concat (GetNuGetDependencies ())
                .Concat (GetNpmDependencies ())
                .ToList ();

            using (var writer = new StreamWriter (JsonOutputFile))
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
            public string LicenseUrl { get; set; }
            public string ProjectUrl { get; set; }
        }

        struct PackageReference : IEquatable<PackageReference>, IComparable<PackageReference>
        {
            public string Id { get; }
            public string Version { get; }

            public PackageReference (string id, string version)
            {
                Id = id;
                Version = version;
            }

            public void Deconstruct (out string id, out string version)
            {
                id = Id;
                version = Version;
            }

            public override string ToString ()
                => string.IsNullOrEmpty (Version) ? Id : $"{Id}/{Version}";

            public int CompareTo (PackageReference other)
            {
                var idCompare = string.Compare (
                    Id, other.Id, StringComparison.OrdinalIgnoreCase);
                if (idCompare == 0)
                    return string.Compare (
                        Version, other.Version, StringComparison.OrdinalIgnoreCase);
                return idCompare;
            }

            public bool Equals (PackageReference other)
                => string.Equals (other.Id, Id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals (other.Version, Version, StringComparison.OrdinalIgnoreCase);

            public override bool Equals (object obj)
                => obj is PackageReference && Equals ((PackageReference)obj);

            public override int GetHashCode ()
                => Id?.GetHashCode () ?? 0 ^ Version?.GetHashCode () ?? 0;
        }
    }
}