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
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Xamarin.MSBuild
{
    sealed class MSBuildProjectFile
    {
        public sealed class FullPathComparer : IEqualityComparer<MSBuildProjectFile>
        {
            public bool Equals (MSBuildProjectFile x, MSBuildProjectFile y)
                => string.Equals (x?.FullPath, y?.FullPath, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode (MSBuildProjectFile obj)
                => obj?.FullPath == null ? 0 : obj.FullPath.GetHashCode ();
        }

        /// <summary>
        /// From from MSBuild's SolutionFile:
        /// https://github.com/Microsoft/msbuild/blob/master/src/Build/Construction/Solution/SolutionFile.cs
        /// </summary>
        static readonly Regex projectLineRegex = new Regex (
            "^" // Beginning of line
            + "Project\\(\"(?<PROJECTTYPEGUID>.*)\"\\)"
            + "\\s*=\\s*" // Any amount of whitespace plus "=" plus any amount of whitespace
            + "\"(?<PROJECTNAME>.*)\""
            + "\\s*,\\s*" // Any amount of whitespace plus "," plus any amount of whitespace
            + "\"(?<RELATIVEPATH>.*)\""
            + "\\s*,\\s*" // Any amount of whitespace plus "," plus any amount of whitespace
            + "\"(?<PROJECTGUID>.*)\""
            + "$", // End-of-line
            RegexOptions.Compiled);

        static readonly Regex normalizePathRegex = new Regex (@"[\\\/]+");

        public static string NormalizePath (string path)
            => normalizePathRegex.Replace (path, Path.DirectorySeparatorChar.ToString ());

        public static IEnumerable<MSBuildProjectFile> ParseProjectsInSolution (string solutionPath)
        {
            if (!File.Exists (solutionPath))
                yield break;

            var solutionDir = Path.GetDirectoryName (solutionPath);

            // yes, this is actually how MSBuild parses solution files: line-by-line matching some regex
            foreach (var line in File.ReadLines (solutionPath)) {
                var projectLine = projectLineRegex.Match (line);
                if (projectLine.Success) {
                    var relativePath = NormalizePath (projectLine.Groups ["RELATIVEPATH"].Value);
                    yield return new MSBuildProjectFile (
                        Path.GetFullPath (Path.Combine (solutionDir, relativePath)),
                        relativePath,
                        projectLine.Groups ["PROJECTNAME"].Value,
                        new Guid (projectLine.Groups ["PROJECTGUID"].Value),
                        new Guid (projectLine.Groups ["PROJECTTYPEGUID"].Value));
                }
            }
        }

        public static readonly Guid SolutionFolderProjectType = new Guid ("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");
        public static readonly Guid CsprojProjectType = new Guid ("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");

        static readonly XNamespace legacyXmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

        public string FullPath { get; }
        public string SolutionRelativePath { get; }
        public string Name { get; }
        public Guid ProjectGuid { get; private set; }
        public Guid ProjectTypeGuid { get; private set; }

        readonly List<PackageReference> packageReferences = new List<PackageReference> ();
        public IReadOnlyList<PackageReference> PackageReferences => packageReferences;

        public MSBuildProjectFile (
            string fullPath,
            string solutionRelativePath,
            string name,
            Guid projectGuid = default (Guid),
            Guid projectTypeGuid = default (Guid))
        {
            FullPath = fullPath;
            SolutionRelativePath = solutionRelativePath;
            Name = name;
            ProjectGuid = projectGuid;
            ProjectTypeGuid = projectTypeGuid;
        }

        public bool IsSolutionFolder => ProjectTypeGuid == SolutionFolderProjectType;

        public void Load (
            bool loadPackageReferences = true,
            bool generateProjectGuidIfMissing = false)
        {
            packageReferences.Clear ();

            var doc = XDocument.Load (FullPath);
            var ns = doc.Root.GetDefaultNamespace ();

            if (generateProjectGuidIfMissing) {
                if (ProjectGuid == Guid.Empty) {
                    var guidString = doc.Descendants (ns + "ProjectGuid").FirstOrDefault ()?.Value;
                    if (guidString != null)
                        ProjectGuid = Guid.Parse (guidString);
                    else
                        ProjectGuid = Guid.NewGuid ();
                }

                if (ProjectTypeGuid == Guid.Empty) {
                    switch (Path.GetExtension (FullPath).ToLowerInvariant ()) {
                    case ".csproj":
                        ProjectTypeGuid = CsprojProjectType;
                        break;
                    }
                }
            }

            if (!loadPackageReferences)
                return;

            foreach (var pr in doc.Descendants (ns + "PackageReference")) {
                var version = pr.Attribute ("Version")?.Value;
                if (version == null)
                    version = pr.Element (ns + "Version")?.Value;

                var ids = pr.Attribute ("Include").Value.Split (
                    new [] { ';' },
                    StringSplitOptions.RemoveEmptyEntries);

                foreach (var packageId in ids)
                    packageReferences.Add (new PackageReference (packageId, version));
            }

            packageReferences.Sort ();
        }

        public struct PackageReference : IEquatable<PackageReference>, IComparable<PackageReference>
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