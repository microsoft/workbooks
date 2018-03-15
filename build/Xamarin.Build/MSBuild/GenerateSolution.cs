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
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class GenerateSolution : Task
    {
        [Required]
        public ITaskItem [] Projects { get; set; }

        [Required]
        public string OutputFile { get; set; }

        public string [] GlobalSectionsFiles { get; set; } = Array.Empty<string> ();
        public string [] Configurations { get; set; } = { "Debug", "Release" };
        public string [] Platforms { get; set; } = { "Any CPU" };

        sealed class SolutionFolder
        {
            public sealed class EqualityComparer : IEqualityComparer<SolutionFolder>
            {
                public bool Equals (SolutionFolder x, SolutionFolder y)
                    => string.Equals (x?.Name, y?.Name, StringComparison.OrdinalIgnoreCase);

                public int GetHashCode (SolutionFolder obj)
                    => obj?.Name?.GetHashCode () ?? 0;
            }

            public string Name { get; set; }
            public Guid Guid { get; set; }

            public bool Equals (SolutionFolder other)
                => string.Equals (Name, other?.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Execute ()
        {
            Directory.CreateDirectory (Path.GetDirectoryName (OutputFile));

            var previousSolutionProjects = MSBuildProjectFile
                .ParseProjectsInSolution (OutputFile)
                .ToDictionary (project => project.SolutionRelativePath);

            var encoding = new UTF8Encoding (true, true);

            var groupedProjects = Projects
                .Select (taskItem => {
                    var relativePath = MSBuildProjectFile.NormalizePath (taskItem.GetMetadata ("RelativePath"));
                    if (!previousSolutionProjects.TryGetValue (relativePath, out var project))
                        project = new MSBuildProjectFile (
                            taskItem.ItemSpec,
                            relativePath,
                            Path.GetFileNameWithoutExtension (taskItem.ItemSpec));
                    return new {
                        SolutionFolderName = taskItem.GetMetadata ("SolutionFolder"),
                        Project = project
                    };
                })
                .GroupBy (
                    project => {
                        var folder = new SolutionFolder {
                            Name = project.SolutionFolderName,
                            Guid = Guid.NewGuid ()
                        };

                        if (previousSolutionProjects.TryGetValue (project.SolutionFolderName, out var prevProject))
                            folder.Guid = prevProject.ProjectGuid;

                        return folder;
                    },
                    project => project.Project,
                    new SolutionFolder.EqualityComparer ())
                .ToList ();

            using (var writer = new StreamWriter (OutputFile, false, encoding) { NewLine = "\r\n" })
                WriteSolution (groupedProjects, writer);

            return true;
        }

        void WriteSolution (
            IReadOnlyList<IGrouping<SolutionFolder, MSBuildProjectFile>> solutionProjects,
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

            void WriteProject (Guid projectTypeGuid, string projectName, string relativePath, Guid projectGuid)
            {
                writer.WriteLine (
                    $"Project(\"{GuidString (projectTypeGuid)}\") = " +
                    $"\"{projectName}\", " +
                    $"\"{FixPath (relativePath)}\", " +
                    $"\"{GuidString (projectGuid)}\"");
                writer.WriteLine ("EndProject");
            }

            foreach (var solutionFolder in solutionProjects) {
                var solutionFolderName = solutionFolder.Key.Name;
                if (!string.IsNullOrEmpty (solutionFolderName))
                    WriteProject (
                        MSBuildProjectFile.SolutionFolderProjectType,
                        solutionFolderName,
                        solutionFolderName,
                        solutionFolder.Key.Guid);

                foreach (var project in solutionFolder) {
                    project.Load (
                        loadPackageReferences: false,
                        generateProjectGuidIfMissing: true);
                    WriteProject (
                        project.ProjectTypeGuid,
                        project.Name,
                        project.SolutionRelativePath,
                        project.ProjectGuid);
                }
            }

            writer.WriteLine ("Global");

            writer.WriteLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            foreach (var configuration in Configurations) {
                foreach (var platform in Platforms)
                    writer.WriteLine ($"\t\t{configuration}|{platform} = {configuration}|{platform}");
            }
            writer.WriteLine ("\tEndGlobalSection");

            writer.WriteLine ("\tGlobalSection(SolutionProperties) = preSolution");
            writer.WriteLine ("\t\tHideSolutionNode = FALSE");
            writer.WriteLine ("\tEndGlobalSection");

            writer.WriteLine ("\tGlobalSection(NestedProjects) = preSolution");
            foreach (var solutionFolder in solutionProjects) {
                var solutionFolderName = solutionFolder.Key.Name;
                if (!string.IsNullOrEmpty (solutionFolderName)) {
                    foreach (var project in solutionFolder)
                        writer.WriteLine (
                            $"\t\t{GuidString (project.ProjectGuid)} = " +
                            $"{GuidString (solutionFolder.Key.Guid)}");
                }
            }
            writer.WriteLine ("\tEndGlobalSection");

            writer.WriteLine ("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (var project in solutionProjects.SelectMany (g => g)) {
                foreach (var configuration in Configurations) {
                    foreach (var platform in Platforms) {
                        writer.WriteLine (
                            $"\t\t{GuidString (project.ProjectGuid)}.{configuration}|{platform}" +
                            $".ActiveCfg = {configuration}|{platform}");
                        writer.WriteLine (
                            $"\t\t{GuidString (project.ProjectGuid)}.{configuration}|{platform}" +
                            $".Build.0 = {configuration}|{platform}");
                    }
                }
            }
            writer.WriteLine ("\tEndGlobalSection");

            foreach (var globalSectionFile in GlobalSectionsFiles)
                writer.WriteLine (File.ReadAllText (globalSectionFile));

            writer.WriteLine ("EndGlobal");
        }
    }
}