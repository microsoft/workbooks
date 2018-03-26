//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

using Should;

using Xamarin.ProcessControl;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Tests
{
    static class WorkbookPackageTestsExtensions
    {
        public static void Save (this WorkbookPackage workbookPackage, FilePath path)
        {
            var saveOperation = workbookPackage.CreateSaveOperation (null);
            saveOperation.Destination = path;
            workbookPackage.Save (saveOperation);
        }
    }

    [TestFixture]
    public class WorkbookPackageTests
    {
        struct WorkbookFile
        {
            public FilePath Path;
        }

        static readonly FilePath PathToSaveAndLoad = TestHelpers.PathToRepoRoot.Combine (
            "Tests",
            "Workbooks",
            "SaveAndLoad"
        );

        static FilePath PathToExternalWorkbooks = TestHelpers.PathToRepoRoot.Combine (
            "Tests",
            "workbooks-repo"
        );

        static readonly string [] InTreeWorkbooks = {
            "SinglePageWithDepsAsDirectory.workbook",
            "SinglePageNoDepsAsDirectory.workbook",
            "SinglePageWithDepsNotAsPackage"
        };

        static readonly FilePath TempRoot = ClientApp
            .SharedInstance
            .FileSystem
            .GetTempDirectory (
                "tests",
                nameof (WorkbookPackageTests));

        static WorkbookPackageTests ()
        {
            void Git (string dir, params string [] args)
                => new Exec (
                    ProcessArguments.FromCommandAndArguments ("git", args),
                    outputRedirection: null,
                    workingDirectory: dir).RunAsync ().GetAwaiter ().GetResult ();

            if (PathToExternalWorkbooks.DirectoryExists) {
                Git (PathToExternalWorkbooks, "pull", "--rebase");
                Git (PathToExternalWorkbooks, "submodule", "sync");
                Git (PathToExternalWorkbooks, "submodule", "update", "--recursive", "--init");
            } else {
                Git (
                    PathToExternalWorkbooks.ParentDirectory,
                    "clone",
                    "--recursive",
                    "https://github.com/xamarin/workbooks",
                    PathToExternalWorkbooks.Name);
            }
        }

        [OneTimeSetUp]
        public void SetUp () => TempRoot.CreateDirectory ();

        [OneTimeTearDown]
        public void TearDown () => Directory.Delete (TempRoot, true);

        [TestCaseSource (nameof (GetWorkbookTestCases))]
        public void OpenAndSaveInPlace (string workbookPath)
        {
            if (new FilePath (workbookPath).DirectoryExists)
                AssertDirectoryWorkbookSaves (workbookPath);
            else
                AssertFileWorkbookSaves (workbookPath);
        }

        [TestCaseSource (nameof (GetWorkbookTestCases))]
        public void OpenAndSaveAs (string workbookPath)
        {
            if (new FilePath (workbookPath).DirectoryExists)
                AssertDirectoryWorkbookSaves (workbookPath, saveAs: true);
            else
                AssertFileWorkbookSaves (workbookPath, saveAs: true);
        }

        [Test]
        public void OpenedArchiveIsRearchivedOnSave ()
        {
            var workbookPath = FilePath.Build (PathToSaveAndLoad, "SinglePageWithDepsAsArchive.workbook");
            workbookPath = GetTemporaryCopyOfWorkbook (workbookPath);

            var workbook = ReadWorkbookDocument (workbookPath);
            var workbookFiles = GetWorkbookFileState (workbook.WorkingPath);

            workbook.Save (workbookPath);

            AssertWorkbookIsReArchived (workbookPath, workbookFiles);
        }

        [Test]
        public void OpenedArchiveDepsAreSaved ()
        {
            var workbookPath = FilePath.Build (PathToSaveAndLoad, "SinglePageWithDepsAsArchive.workbook");
            workbookPath = GetTemporaryCopyOfWorkbook (workbookPath);
            var savePath = workbookPath.ParentDirectory.Combine ("NewWorkbookPath.workbook");

            var workbook = ReadWorkbookDocument (workbookPath);
            var workbookFiles = GetWorkbookFileState (workbook.WorkingPath);

            workbook.Save (savePath);

            AssertWorkbookIsReArchived (savePath, workbookFiles);
        }

        #region Assertions

        void AssertWorkbookIsReArchived (FilePath workbookPath, IEnumerable<WorkbookFile> workbookFileState)
        {
            workbookPath.FileExists.ShouldBeTrue ();
            workbookPath.DirectoryExists.ShouldBeFalse ();

            var extractPath = TempRoot.GetTempFileName ("workbook");
            var stream = workbookPath.OpenRead ();
            var zipArchive = new ZipArchive (
                stream,
                ZipArchiveMode.Read,
                true,
                Encoding.UTF8);
            zipArchive.Extract (extractPath, preserveRootDirectory: false);
            stream.Dispose ();

            AssertDirectoryWorkbookUnchanged (extractPath, workbookFileState);
        }

        void AssertDirectoryWorkbookSaves (FilePath workbookPath, bool saveAs = false)
        {
            var workbookFiles = GetWorkbookFileState (workbookPath);

            // Copy the whole thing to a temp directory for idempotence.
            workbookPath = GetTemporaryCopyOfWorkbook (workbookPath);

            var targetPath = saveAs ? workbookPath.ParentDirectory.Combine ("NewWorkbook.workbook")
                : workbookPath;

            var workbookPackage = ReadWorkbookDocument (workbookPath);
            workbookPackage.Save (targetPath);
            AssertDirectoryWorkbookUnchanged (targetPath, workbookFiles);
        }

        void AssertFileWorkbookSaves (FilePath workbookPath, bool saveAs = false)
        {
            var workbookFileState = new WorkbookFile {
                Path = workbookPath.Name
            };

            workbookPath = GetTemporaryCopyOfWorkbook (workbookPath);

            var targetPath = saveAs ? workbookPath.ParentDirectory.Combine ("NewWorkbook.workbook")
                : workbookPath;

            var workbookPackage = ReadWorkbookDocument (workbookPath);
            workbookPackage.Save (targetPath);
            AssertFileWorkbookUnchanged (targetPath, workbookFileState);
        }

        void AssertFileWorkbookUnchanged (FilePath workbookPath, WorkbookFile workbookFileState)
        {
            workbookPath.FileExists.ShouldBeTrue ($"{workbookPath} no longer exists after saving.");
        }

        void AssertDirectoryWorkbookUnchanged (FilePath workbookPath, IEnumerable<WorkbookFile> files)
        {
            foreach (var file in files) {
                var targetPath = workbookPath.Combine (file.Path);

                targetPath.FileExists.ShouldBeTrue (
                    $"{targetPath.Name} does not exist in {workbookPath} after saving.");
            }
        }

        #endregion

        #region Helpers

        WorkbookPackage ReadWorkbookDocument (string path)
        {
            var workbook = new WorkbookPackage ();
            workbook.Open (quarantineInfo => Task.FromResult (true), path).Wait ();
            return workbook;
        }

        FilePath GetTemporaryCopyOfWorkbook (
            FilePath workbookPath,
            [CallerMemberName] string caller = "")
        {
            var tempDirectory = GetTempDirectory (caller).GetTempFileName ();
            if (workbookPath.DirectoryExists) {
                CopyDirectory (workbookPath, tempDirectory);
                return tempDirectory.Combine (workbookPath.Name);
            } else {
                tempDirectory.CreateDirectory ();
                var targetPath = tempDirectory.Combine (workbookPath.Name);
                File.Copy (workbookPath, targetPath);
                return targetPath;
            }
        }

        FilePath GetTempDirectory ([CallerMemberName] string caller = "") =>
            new FilePath (TempRoot.Combine (caller).CreateDirectory ().FullName);

        IEnumerable<WorkbookFile> GetWorkbookFileState (FilePath workbookPath)
            => workbookPath.EnumerateFiles (searchOption: SearchOption.AllDirectories)
                .Select (fp => new WorkbookFile {
                    Path = fp.GetRelativePath (workbookPath)
                }).ToArray ();

        void CopyDirectory (FilePath sourceDirectory, FilePath targetDirectory)
        {
            sourceDirectory.DirectoryExists.ShouldBeTrue ();
            targetDirectory.CreateDirectory ();

            var dirName = sourceDirectory.Name;

            var realTarget = targetDirectory.Combine (dirName);
            realTarget.CreateDirectory ();

            var dirInfo = sourceDirectory.CreateDirectory ();

            foreach (var file in dirInfo.GetFiles ())
                file.CopyTo (realTarget.Combine (file.Name));

            foreach (var subdir in dirInfo.GetDirectories ())
                CopyDirectory (subdir.FullName, realTarget);
        }

        string HashFile (FilePath file, string hash = "SHA-256")
        {
            using (var hasher = HashAlgorithm.Create (hash)) {
                var computedHash = hasher.ComputeHash (file.OpenRead ());
                return BitConverter.ToString (computedHash).Replace ("-", "").ToLowerInvariant ();
            }
        }

        static IEnumerable<ITestCaseData> GetWorkbookTestCases ()
        {
            foreach (var workbook in InTreeWorkbooks)
                yield return new TestCaseData (PathToSaveAndLoad.Combine (workbook).ToString ()) {
                    TestName = new FilePath (workbook).Name
                };

            foreach (var workbook in GetExternalWorkbookTestCases ())
                yield return new TestCaseData (workbook.FullPath) { TestName = workbook.Name };
        }

        static IEnumerable<FilePath> GetExternalWorkbookTestCases ()
        {
            if (!PathToExternalWorkbooks.DirectoryExists)
                return Array.Empty<FilePath> ();

            var workbooks = new List<FilePath> ();
            var stack = new Stack<FilePath> (PathToExternalWorkbooks.EnumerateDirectories ());

            while (stack.Count > 0) {
                var topDir = stack.Pop ();

                // If this directory _is_ a workbook directory, add it and continue.
                if (topDir.Extension == ".workbook") {
                    workbooks.Add (topDir);
                    continue;
                }

                // Add all single-file workbooks from this directory.
                workbooks.AddRange (topDir.EnumerateFiles ("*.workbook"));

                foreach (var subdir in topDir.EnumerateDirectories ()) {
                    if (subdir.Extension == ".workbook")
                        workbooks.Add (subdir);
                    else
                        stack.Push (subdir);
                }
            }

            return workbooks;
        }

        #endregion
    }
}