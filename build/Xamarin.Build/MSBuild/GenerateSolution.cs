//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class GenerateSolution : Task
    {
        [Required]
        public string ProjectsRelativeToPath { get; set; }

        [Required]
        public ITaskItem [] Projects { get; set; }

        [Required]
        public string OutputFile { get; set; }

        public string SolutionConfiguration { get; set; } = "Debug";
        public string SolutionPlatform { get; set; } = "Any CPU";

        public string [] GlobalSectionsFiles { get; set; } = Array.Empty<string> ();

        public struct ProjectConfiguration
        {
            public string ConfigurationName { get; }
            public string PlatformName { get; }

            public ProjectConfiguration (string configurationName, string platformName)
            {
                ConfigurationName = configurationName;
                PlatformName = platformName;
            }

            public override string ToString ()
                => $"{ConfigurationName}|{PlatformName}";
        }

        sealed class SolutionNode
        {
            static readonly Guid rootGuid = new Guid ("{9EE49C97-D40B-4B4C-8F17-2C5D6A99AC3E}");
            static readonly Guid solutionFolderTypeGuid = new Guid ("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");
            static readonly Guid csprojTypeGuid = new Guid ("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
            static readonly Guid fsprojTypeGuid = new Guid ("{F2A71F9B-5D33-465A-A702-920D77279786}");
            static readonly Guid vbprojTypeGuid = new Guid ("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}");
            static readonly Guid shprojTypeGuid = new Guid ("{D954291E-2A0B-460D-934E-DC6B0785DB48}");

            public SolutionNode Top { get; }
            public SolutionNode Parent { get; }
            public string Name { get; }
            public string RelativePath { get; }
            public Project Project { get; }
            public ProjectConfiguration Configuration { get; }
            public Guid Guid { get; }
            public Guid TypeGuid { get; }

            public SolutionNode ()
            {
                Top = this;
                Guid = rootGuid;
            }

            SolutionNode (
                SolutionNode top,
                SolutionNode parent,
                string folderName)
            {
                Top = top ?? throw new ArgumentNullException (nameof (top));
                Parent = parent ?? throw new ArgumentNullException (nameof (parent));
                Name = folderName ?? throw new ArgumentNullException (nameof (folderName));
                RelativePath = folderName;
                Guid = CreateVersion5Guid (parent.Guid, folderName);
                TypeGuid = solutionFolderTypeGuid;
            }

            SolutionNode (
                SolutionNode top,
                SolutionNode parent,
                Guid projectGuid,
                Project project,
                ProjectConfiguration configuration,
                string relativePath)
            {
                Top = top ?? throw new ArgumentNullException (nameof (top));
                Parent = parent ?? throw new ArgumentNullException (nameof (parent));
                Guid = projectGuid;
                Project = project ?? throw new ArgumentNullException (nameof (project));
                Configuration = configuration;
                Name = Path.GetFileNameWithoutExtension (project.FullPath);
                RelativePath = relativePath;

                switch (Path.GetExtension (project.FullPath).ToLowerInvariant ()) {
                case ".csproj":
                    TypeGuid = csprojTypeGuid;
                    break;
                case ".fsproj":
                    TypeGuid = fsprojTypeGuid;
                    break;
                case ".vbproj":
                    TypeGuid = vbprojTypeGuid;
                    break;
                case ".shproj":
                    TypeGuid = shprojTypeGuid;
                    break;
                }
            }

            readonly List<SolutionNode> children = new List<SolutionNode> ();
            public IReadOnlyList<SolutionNode> Children => children;

            public SolutionNode GetOrAddChild (string folderName)
            {
                var child = children.Find (c =>
                    c.Project == null &&
                    string.Equals (c.Name, folderName, StringComparison.Ordinal));

                if (child == null)
                    children.Add (child = new SolutionNode (Top, this, folderName));

                return child;
            }

            public SolutionNode AddChild (
                Guid projectGuid,
                Project project,
                ProjectConfiguration configuration,
                string relativePath)
            {
                var child = new SolutionNode (
                    Top,
                    this,
                    projectGuid,
                    project,
                    configuration,
                    relativePath);
                children.Add (child);
                return child;
            }
        }

        public override bool Execute ()
        {
            Directory.CreateDirectory (Path.GetDirectoryName (OutputFile));

            var projects = new ProjectCollection (new Dictionary<string, string> {
                ["Configuration"] = SolutionConfiguration
            });

            void LoadProject (string path)
            {
                var project = projects.LoadProject (path);
                foreach (var projectReference in project.GetItems ("ProjectReference")) {
                    var referencePath = Path.GetFullPath (Path.Combine (
                        project.DirectoryPath,
                        projectReference.EvaluatedInclude));
                    if (!projects.GetLoadedProjects (referencePath).Any ())
                        LoadProject (referencePath);
                }
            }

            foreach (var project in Projects)
                LoadProject (project.ItemSpec);

            var solutionFolderBaseUri = new Uri (Path.GetFullPath (ProjectsRelativeToPath));
            var solutionBaseUri = new Uri (Path.GetFullPath (OutputFile));
            var solution = new SolutionNode ();

            foreach (var project in projects.LoadedProjects) {
                var projectUri = new Uri (Path.GetFullPath (project.FullPath));
                var solutionFolderRelativePath = solutionFolderBaseUri.MakeRelativeUri (projectUri).OriginalString;
                var solutionRelativePath = solutionBaseUri.MakeRelativeUri (projectUri).OriginalString;
                var solutionNodeNames = Path.GetDirectoryName (Path.GetDirectoryName (solutionFolderRelativePath)).Split (
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

                var parent = solution;
                foreach (var name in solutionNodeNames)
                    parent = parent.GetOrAddChild (name);

                var explicitProjectGuid = project.GetPropertyValue ("ProjectGuid");
                if (string.IsNullOrEmpty (explicitProjectGuid) ||
                    !Guid.TryParse (explicitProjectGuid, out var projectGuid))
                    projectGuid = CreateVersion5Guid (solution.Guid, solutionFolderRelativePath);

                var explicitConfiguration = project.GetPropertyValue ("Configuration");
                if (string.IsNullOrEmpty (explicitConfiguration))
                    explicitConfiguration = SolutionConfiguration;

                var explicitPlatform = project.GetPropertyValue ("Platform");
                if (string.IsNullOrEmpty (explicitPlatform) || explicitPlatform == "AnyCPU")
                    explicitPlatform = SolutionPlatform;

                parent.AddChild (
                    projectGuid,
                    project,
                    new ProjectConfiguration (explicitConfiguration, explicitPlatform),
                    solutionRelativePath);
            }

            var encoding = new UTF8Encoding (true, true);
            using (var writer = new StreamWriter (OutputFile, false, encoding)
                { NewLine = "\r\n" })
                WriteSolution (solution, writer);

            return true;
        }

        void WriteSolution (
            SolutionNode solution,
            StreamWriter writer)
        {
            string FixPath (string path)
                => path.Replace ('/', '\\');

            string GuidString (Guid guid)
                => guid.ToString ("B").ToUpperInvariant ();

            var basePath = Path.GetDirectoryName (OutputFile);

            writer.WriteLine ();
            writer.WriteLine ("Microsoft Visual Studio Solution File, Format Version 12.00");
            writer.WriteLine ("# Visual Studio 15");
            writer.WriteLine ("VisualStudioVersion = 15.0.26124.0");
            writer.WriteLine ("MinimumVisualStudioVersion = 15.0.26124.0");

            void WriteAllNodes (SolutionNode node)
            {
                if (node != node.Top) {
                    writer.WriteLine (
                        $"Project(\"{GuidString (node.TypeGuid)}\") = " +
                        $"\"{node.Name}\", " +
                        $"\"{FixPath (node.RelativePath)}\", " +
                        $"\"{GuidString (node.Guid)}\"");
                    writer.WriteLine ("EndProject");
                }

                foreach (var child in node.Children)
                    WriteAllNodes (child);
            }

            WriteAllNodes (solution);

            writer.WriteLine ("Global");

            writer.WriteLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            writer.WriteLine ($"\t\t{SolutionConfiguration}|{SolutionPlatform} = {SolutionConfiguration}|{SolutionPlatform}");
            writer.WriteLine ("\tEndGlobalSection");

            writer.WriteLine ("\tGlobalSection(SolutionProperties) = preSolution");
            writer.WriteLine ("\t\tHideSolutionNode = FALSE");
            writer.WriteLine ("\tEndGlobalSection");

            writer.WriteLine ("\tGlobalSection(NestedProjects) = preSolution");

            void WriteNestedProjects (SolutionNode node)
            {
                if (node != node.Top && node.Parent != node.Top)
                    writer.WriteLine (
                        $"\t\t{GuidString (node.Guid)} = " +
                        $"{GuidString (node.Parent.Guid)}");

                foreach (var child in node.Children)
                    WriteNestedProjects (child);
            }

            WriteNestedProjects (solution);

            writer.WriteLine ("\tEndGlobalSection");

            writer.WriteLine ("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            void WriteConfigurations (SolutionNode node)
            {
                if (node.Project != null) {
                    writer.WriteLine (
                        $"\t\t{GuidString (node.Guid)}.{SolutionConfiguration}|{SolutionPlatform}" +
                        $".ActiveCfg = {node.Configuration}");
                    writer.WriteLine (
                        $"\t\t{GuidString (node.Guid)}.{SolutionConfiguration}|{SolutionPlatform}" +
                        $".Build.0 = {node.Configuration}");
                }

                foreach (var child in node.Children)
                    WriteConfigurations (child);
            }

            WriteConfigurations (solution);

            writer.WriteLine ("\tEndGlobalSection");

            foreach (var globalSectionFile in GlobalSectionsFiles)
                writer.WriteLine (File.ReadAllText (globalSectionFile).TrimEnd ());

            writer.WriteLine ("EndGlobal");
        }

        static Guid CreateVersion5Guid (Guid namespaceGuid, string name)
        {
            if (name == null)
                throw new ArgumentNullException (nameof (name));

            using (var sha1 = SHA1.Create ()) {
                var namespaceBytes = namespaceGuid.ToByteArray ();
                var nameBytes = Encoding.UTF8.GetBytes (name);
                Swap (namespaceBytes);

                sha1.TransformBlock (namespaceBytes, 0, namespaceBytes.Length, null, 0);
                sha1.TransformFinalBlock (nameBytes, 0, nameBytes.Length);

                var guid = new byte [16];
                Array.Copy (sha1.Hash, 0, guid, 0, 16);

                guid [6] = (byte)((guid [6] & 0x0F) | 0x50);
                guid [8] = (byte)((guid [8] & 0x3F) | 0x80);

                Swap (guid);
                return new Guid (guid);
            }

            void Swap (byte [] g)
            {
                void SwapAt (int left, int right)
                {
                    var t = g [left];
                    g [left] = g [right];
                    g [right] = t;
                }

                SwapAt (0, 3);
                SwapAt (1, 2);
                SwapAt (4, 5);
                SwapAt (6, 7);
            }
        }
    }
}