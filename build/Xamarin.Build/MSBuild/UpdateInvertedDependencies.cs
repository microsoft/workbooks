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
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Xamarin.Interactive.Markdown;

using SIOCZipArchive = System.IO.Compression.ZipArchive;

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

        public string MarkdownOutputFile { get; set; }

        sealed class NuGetResource
        {
            [JsonProperty ("@id")]
            public string Id { get; set; }

            [JsonProperty ("@type")]
            public string Type { get; set; }

            [JsonProperty ("comment")]
            public string Comment { get; set; }
        }

        sealed class NuGetResources
        {
            [JsonProperty ("version")]
            public string Version { get; set; }

            [JsonProperty ("resources")]
            public NuGetResource [] Resources { get; set; }
        }

        HttpClient httpClient;

        (Stream stream, string contentType) HttpGet (string uri)
        {
            if (httpClient == null)
                httpClient = new HttpClient ();

            Log.LogMessage ("-> HTTP GET {0}", uri);

            var response = httpClient.GetAsync (uri).GetAwaiter ().GetResult ();

            Log.LogMessage (
                "   {0} ({1}) {2}: {3} {4} bytes",
                response.StatusCode,
                (int)response.StatusCode,
                response.ReasonPhrase,
                response.Content.Headers.ContentType,
                response.Content.Headers.ContentLength);

            response.EnsureSuccessStatusCode ();

            return (
                response.Content.ReadAsStreamAsync ().GetAwaiter ().GetResult (),
                response.Content.Headers.ContentType.MediaType
            );
        }

        IEnumerable<InvertedDependency> GetNuGetDependencies ()
        {
            var packagesToProjects = new Dictionary<PackageReference, List<Project>> ();
            var packageRoots = new HashSet<string> ();
            List<string> nugetFlatContainerBaseUris = null;

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

            string MakePackagePath (PackageReference packageReference)
                => Path.Combine (
                    packageReference.Id,
                    packageReference.Version,
                    packageReference.Id + ".nuspec");

            string GetNuSpecPath (PackageReference packageReference)
            {
                var packagePath = MakePackagePath (packageReference);

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

            void DownloadNuSpec (PackageReference packageReference)
            {
                if (NuGetTool == null || NuspecCacheDirectory == null)
                    return;

                if (nugetFlatContainerBaseUris == null) {
                    var serializer = JsonSerializer.CreateDefault ();
                    nugetFlatContainerBaseUris = Exec
                        .Run (NuGetTool, "sources", "-Format", "Short")
                        .Where (source => source.StartsWith ("E ", StringComparison.Ordinal))
                        .Select (source => source.Substring (2))
                        .Select (source => {
                            try {
                                var (stream, contentType) = HttpGet (source);
                                using (stream)
                                using (var streamReader = new StreamReader (stream))
                                using (var jsonReader = new JsonTextReader (streamReader))
                                    return serializer
                                        .Deserialize<NuGetResources> (jsonReader)
                                        .Resources
                                        .Where (resource => resource.Type == "PackageBaseAddress/3.0.0")
                                        .Select (resource => resource.Id)
                                        .SingleOrDefault ();
                            } catch (HttpRequestException) {
                                return null;
                            }
                        })
                        .Where (source => source != null)
                        .ToList ();
                }

                foreach (var baseUri in nugetFlatContainerBaseUris) {
                    var relativePath = MakePackagePath (packageReference).ToLowerInvariant ();
                    var localPath = Path.Combine (NuspecCacheDirectory, relativePath);
                    var uri = $"{baseUri}{relativePath.Replace (Path.DirectorySeparatorChar, '/')}";

                    try {
                        Directory.CreateDirectory (Path.GetDirectoryName (localPath));

                        var (stream, contentType) = HttpGet (uri);
                        using (stream) {
                            if (contentType.EndsWith ("/xml", StringComparison.OrdinalIgnoreCase)) {
                                using (var fileStream = File.Create (localPath))
                                    stream.CopyTo (fileStream);
                            } else {
                                // MyGet does not implement NuGet v3 PackageBaseAddress/3.0.0 at all and will
                                // only and always return the nupkg as content and never the nuspec :(
                                // https://twitter.com/MyGetTeam/status/1011688120121810944
                                Log.LogMessage ("-> non-nuspec/XML detected; assuming nupkg archive");
                                using (var archive = new SIOCZipArchive (stream, ZipArchiveMode.Read))
                                    archive
                                        .Entries
                                        .First (entry => string.Equals (
                                            entry.FullName,
                                            packageReference.Id + ".nuspec",
                                            StringComparison.OrdinalIgnoreCase))
                                        .ExtractToFile (
                                            localPath,
                                            overwrite: true);
                            }
                        }

                        break;
                    } catch (HttpRequestException) {
                    }
                }
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
                    var invertedDependency = InvertedDependency.Create (
                        this,
                        DependencyKind.NuGet,
                        packageReference.Id,
                        packageReference.Version);

                    invertedDependency.DependentProjects = packagesToProjects [packageReference]
                        .Select (p => Path.GetFileNameWithoutExtension (p.FullPath))
                        .ToArray ();

                    var nuspecPath = GetNuSpecPath (packageReference);
                    if (nuspecPath == null) {
                        DownloadNuSpec (packageReference);
                        nuspecPath = GetNuSpecPath (packageReference);
                    }

                    if (nuspecPath != null) {
                        Log.LogMessage ("-> Parsing nuspec {0}", nuspecPath);
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
                .Select (e => InvertedDependency.Create (
                    this,
                    DependencyKind.Npm,
                    e.Name.ToString (),
                    e.Value))
                ).OrderBy (e => e.Name).ThenBy (e => e.Version);
        }

        IEnumerable<InvertedDependency> GetGitSubmoduleDependencies ()
        {
            foreach (var line in Exec.Run ("git", "submodule", "status")) {
                var parts = line.Split (
                    new [] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts == null || parts.Length < 2)
                    continue;

                yield return InvertedDependency.Create (
                    this,
                    DependencyKind.GitSubmodule,
                    parts [1],
                    parts [0]);
            }
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

            var links = new Dictionary<string, int> ();

            string MarkdownLink (string linkText, string linkTarget, string nolinkText)
            {
                if (linkTarget == null)
                    return nolinkText ?? linkText;

                if (!links.TryGetValue (linkTarget, out var count))
                    links.Add (linkTarget, count = links.Count + 1);

                return $"[{linkText}][{count}]";
            }

            if (MarkdownOutputFile != null) {
                var table = new MarkdownTable (
                    "Dependencies",
                    "Kind",
                    "Name",
                    "Version",
                    "License");

                foreach (var dependency in invertedDependencies)
                    table.Add (
                        dependency.Kind.ToString (),
                        MarkdownLink (dependency.Name, dependency.ProjectUrl, dependency.Name),
                        dependency.Version,
                        MarkdownLink ("License", dependency.LicenseUrl, string.Empty));

                using (var writer = new StreamWriter (MarkdownOutputFile)) {
                    table.Render (writer);
                    writer.WriteLine ();
                    foreach (var link in links)
                        writer.WriteLine ($"[{link.Value}]: {link.Key}");
                }
            }

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

            public static InvertedDependency Create (
                Task task,
                DependencyKind kind,
                string name,
                string version)
            {
                task.Log.LogMessage (
                    MessageImportance.High,
                    "Processing {0} {1}/{2}",
                    kind,
                    name,
                    version);

               return new InvertedDependency {
                    Kind = kind,
                    Name = name,
                    Version = version
                };
            }
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