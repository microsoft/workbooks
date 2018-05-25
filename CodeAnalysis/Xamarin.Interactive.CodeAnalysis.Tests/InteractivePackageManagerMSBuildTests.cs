// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Microsoft.Build.Evaluation;

using Xunit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Tests;
using Xamarin.ProcessControl;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    public class InteractivePackageManagerMSBuildTests
    {
        static readonly FilePath ThisTestProjectDirectory = new FilePath (
            typeof (InteractivePackageManagerMSBuildTests).Assembly.Location)
                .ParentDirectory
                .ParentDirectory
                .ParentDirectory
                .ParentDirectory;

        static readonly FilePath TestProjectsDirectory = ThisTestProjectDirectory.Combine ("TestProjects");

        static string NormalizePath (string path)
            => path
                ?.Replace ('\\', Path.DirectorySeparatorChar)
                ?.Replace ('/', Path.DirectorySeparatorChar);

        /// <summary>
        /// Perform a restore operation against a project through MSBuild and our own
        /// InteractivePackageManager API to ensure we do not differ from MSBuild.
        /// </summary>
        /// <remarks>
        /// Restores, builds, and parses isolated projects with MSBuild. The projects
        /// are loaded into an in-process MSBuild to evaluate the project file, extract
        /// some properties and all PackageReferences, and translate those packages
        /// into a restore operation as it would happen within workbooks using our own
        /// NuGet stack. Once both the MSBuild restore/build and our own restore are
        /// complete, the assembly lists are compared. The test fails if there are any
        /// differences.
        /// </remarks>
        // [Theory]
        // [InlineData ("AspNetCoreMvc", "Publish", "Release", "publish")]
        public async Task MSBuildAndInteractiveResolveSameAssemblies (
            string projectName,
            string target,
            string configuration,
            string assemblyOutputDirectory)
        {
            // Configure and assert some preconditions
            Assert.NotNull (projectName);
            Assert.NotNull (target);
            Assert.NotNull (configuration);

            var projectDirectory = TestProjectsDirectory.Combine (projectName);
            Assert.True (projectDirectory.Exists);

            var projectFile = projectDirectory.Combine (projectName + ".csproj");
            Assert.True (projectFile.FileExists);

            // Global properties will be used to build the project and
            // to evaluate the project in this unit test context.
            var globalProperties = new Dictionary<string, string> {
                ["Configuration"] = configuration ?? "Release"
            };

            // Invoke dotnet msbuild to restore and build the project
            var processArguments = ProcessArguments.Create (
                Which.Exec ("dotnet"),
                "msbuild",
                projectFile,
                "/restore",
                "/t:" + target
            );

            foreach (var property in globalProperties)
                processArguments = processArguments.Add ($"/p:{property.Key}={property.Value}");

            var output = new StringWriter ();
            try {
                await new Exec (
                    processArguments,
                    ExecFlags.Default | ExecFlags.RedirectStdout | ExecFlags.RedirectStderr,
                    outputHandler: segment => output.Write (segment)).RunAsync ();
            } catch {
                Assert.False (true, output.ToString ());
            }

            // Load and evaluate the project so we can read items/properties
            var projectCollection = new ProjectCollection (globalProperties);
            var project = projectCollection.LoadProject (projectFile);

            // Read and assert a number of properties from the project that we'll need later
            var assemblyName = project.GetPropertyValue ("AssemblyName");
            Assert.NotNull (assemblyName);
            assemblyName += ".dll";

            var targetFrameworkMoniker = project.GetPropertyValue ("TargetFrameworkMoniker");
            Assert.NotNull (targetFrameworkMoniker);

            Console.WriteLine (targetFrameworkMoniker);

            var outputPath = project.GetPropertyValue ("OutputPath");
            Assert.NotNull (outputPath);
            outputPath = NormalizePath (outputPath);

            // Read all NuGet package references from the project and translate them into a
            // set of package descriptions that we'll restore using InteractivePackageManager.
            var packages = PackageReferenceList.Create (project
                .AllEvaluatedItems
                .Where (item => item.ItemType == "PackageReference")
                .Select (item => {
                    Assert.NotNull (item.EvaluatedInclude);

                    var version = item.GetMetadataValue ("Version");
                    Assert.NotNull (version);

                    Console.WriteLine (item.EvaluatedInclude);

                    return new InteractivePackageDescription (
                        item.EvaluatedInclude,
                        version);
                }));

            // Create InteractivePackageManager with the same TFM from the project
            var packageManager = InteractivePackageManagerTests.CreatePackageManager (
                new FrameworkName (targetFrameworkMoniker));

            // Perform the actual restore
            var installedPackages = await packageManager.RestoreAsync (packages, default);

            // Resolve all assemblies from the installed set of packages
            var actualAssemblies = installedPackages
                .SelectMany (installedPackage => installedPackage.AssemblyReferences)
                .Distinct ()
                .Select (p => p.Name)
                .ToList ();
            actualAssemblies.Sort ();

            // Ensure the output directory is in place as expected for the built MSBuild project
            var expectedAssembliesDirectory = projectDirectory.Combine (outputPath);
            if (assemblyOutputDirectory != null)
                expectedAssembliesDirectory = expectedAssembliesDirectory.Combine (assemblyOutputDirectory);
            Assert.True (expectedAssembliesDirectory.DirectoryExists);

            // Gather all the assemblies that MSBuild published; this is normally the result
            // of the "Publish" target for a .NET Core app, for example - filter out the
            // actual application assembly.
            var expectedAssemblies = expectedAssembliesDirectory
                .EnumerateFiles ("*.dll")
                .Select (p => p.Name)
                .Where (p => p != assemblyName)
                .ToList ();
            expectedAssemblies.Sort ();

            // Render a nice +/- diff if there is one for quicker debugging
            var diff = new DiffRenderer (expectedAssemblies, actualAssemblies);
            Assert.False (diff.HasDiff, diff.ToString ());

            // And a final sanity assert
            Assert.Equal (expectedAssemblies, actualAssemblies);
        }
    }
}