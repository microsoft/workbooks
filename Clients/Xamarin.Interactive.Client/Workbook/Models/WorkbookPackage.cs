//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.IO;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.TreeModel;
using Xamarin.Interactive.Workbook.LoadAndSave;
using Xamarin.Interactive.Workbook.Structure;

namespace Xamarin.Interactive.Workbook.Models
{
    sealed class WorkbookPackage : IWorkbookTitledNode, INotifyPropertyChanged
    {
        const string TAG = nameof (WorkbookPackage);

        const string extension = "workbook";
        const string dottedExtension = "." + extension;
        const string indexPageFileName = "index." + extension;

        static readonly FilePath privateWorkingRootPath = ClientApp
            .SharedInstance
            .FileSystem
            .GetTempDirectory ("working-root");

        public static bool IsPossiblySupported (FilePath path)
        {
            if (path.Extension != dottedExtension)
                return false;

            if (path.FileExists)
                return true;

            if (path.DirectoryExists && path.Combine (indexPageFileName).FileExists)
                return true;

            return false;
        }

        public EditorHub<Cell> EditorHub { get; } = new EditorHub<Cell> ();

        public event EventHandler<PathChangedEventArgs> WorkingPathChanged;
        public event EventHandler<PathChangedEventArgs> LogicalPathChanged;

        bool openedFromDirectory;

        FilePath workingPath;
        public FilePath WorkingPath {
            get { return workingPath; }
            private set {
                if (workingPath != value) {
                    var oldPath = workingPath;
                    workingPath = value;
                    WorkingPathChanged?.Invoke (
                        this,
                        new PathChangedEventArgs (oldPath, workingPath));
                }
            }
        }

        FilePath logicalPath;
        public FilePath LogicalPath {
            get { return logicalPath; }
            private set {
                if (logicalPath != value) {
                    var oldPath = logicalPath;
                    logicalPath = value;
                    LogicalPathChanged?.Invoke (
                        this,
                        new PathChangedEventArgs (oldPath, logicalPath));
                }
            }
        }

        public FilePath WorkingBasePath => WorkingPath.DirectoryExists
            ? WorkingPath
            : WorkingPath.ParentDirectory;

        public TreeNode TreeNode { get; }

        readonly NuGetPackagesNode nugetPackagesNode = new NuGetPackagesNode ();
        readonly Collections.ObservableCollection<FileNode> filesystem =
            new Collections.ObservableCollection<FileNode> ();

        readonly List<WorkbookPage> pages = new List<WorkbookPage> ();
        public IReadOnlyList<WorkbookPage> Pages => pages;

        public WorkbookSaveOptions SaveOptions { get; private set; }

        public WorkbookPage IndexPage => Pages.FirstOrDefault ();

        public ImmutableArray<AgentType> PlatformTargets { get; private set; }
            = ImmutableArray<AgentType>.Empty;

        InteractivePackageManager packages;
        public InteractivePackageManager Packages {
            get { return packages; }
            set {
                if (packages == value)
                    return;

                if (packages != null)
                    packages.PropertyChanged -= Packages_PropertyChanged;

                packages = value;

                if (packages != null)
                    packages.PropertyChanged += Packages_PropertyChanged;
            }
        }

        public string DefaultTitle => logicalPath.Exists
            ? logicalPath.NameWithoutExtension
            : WorkbookPage.DefaultTitle;

        public string Title {
            get { return IndexPage == null || IndexPage.IsUntitled ? DefaultTitle : IndexPage.Title; }
            set {
                if (IndexPage != null)
                    IndexPage.Title = value;
            }
        }

        public bool IsDirty => pages.Any (page => page.IsDirty);

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        #endregion

        public WorkbookPackage (FilePath pendingOpenPath = default (FilePath))
        {
            logicalPath = pendingOpenPath;

            var children = new Collections.AggregateObservableCollection<TreeNode> ();
            children.AddSource (new [] { nugetPackagesNode });
            children.AddSource (filesystem);

            TreeNode = new WorkbookTitledNode (this) {
                IsExpanded = true,
                IsRenamable = false,
                IconName = "solution",
                Children = children
            };
        }

        void WorkbookPage_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (sender == IndexPage) {
                switch (e.PropertyName) {
                case nameof (WorkbookPage.Title):
                    NotifyPropertyChanged (e.PropertyName);
                    break;
                }
            }
        }

        void Packages_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof (InteractivePackageManager.InstalledPackages))
                nugetPackagesNode.UpdateChildren (Packages.InstalledPackages.Where (p => p.IsExplicit));
        }

        public IReadOnlyList<LanguageDescription> GetLanguageDescriptions ()
            => Pages
                .SelectMany (p => p.Contents)
                .OfType<CodeCell> ()
                .Select (cell => cell.LanguageName.ToLowerInvariant ())
                .Distinct ()
                .Select (name => new LanguageDescription (name))
                .ToList ();

        public void AddPage (WorkbookPage workbookPage)
        {
            if (workbookPage == null)
                throw new ArgumentNullException (nameof (workbookPage));

            pages.Add (workbookPage);

            if (pages.Count == 1)
                workbookPage.TreeNode.IsSelected = true;

            workbookPage.PropertyChanged += WorkbookPage_PropertyChanged;

            filesystem.Add (workbookPage.TreeNode);

            if (pages.Count == 1)
                PlatformTargets = workbookPage.PlatformTargets;
            else
                PlatformTargets = PlatformTargets
                    .Intersect (workbookPage.PlatformTargets)
                    .ToImmutableArray ();
        }

        public void RemovePage (WorkbookPage workbookPage)
        {
            if (workbookPage == null)
                throw new ArgumentNullException (nameof (workbookPage));

            workbookPage.PropertyChanged -= WorkbookPage_PropertyChanged;

            pages.Remove (workbookPage);

            if (pages.Count == 0) {
                PlatformTargets = ImmutableArray<AgentType>.Empty;
            } else {
                PlatformTargets = pages [0].PlatformTargets;

                if (pages.Count > 1) {
                    var intersection = new HashSet<AgentType> (PlatformTargets);
                    foreach (var page in pages.Skip (1))
                        intersection.IntersectWith (page.PlatformTargets);

                    PlatformTargets = intersection.ToImmutableArray ();
                }
            }
        }

        public void Close ()
        {
            if (WorkingPath.DirectoryExists && WorkingPath.IsChildOfDirectory (privateWorkingRootPath)) {
                Log.Info (TAG, $"Deleting private working path {WorkingPath}");
                try {
                    Directory.Delete (WorkingPath, true);
                } catch (Exception e) {
                    Log.Error (TAG, $"Unable to delete private working copy {WorkingPath}", e);
                }
            }
        }

        public delegate Task<bool> QuarantineInfoHandler (QuarantineInfo quarantineInfo);

        public async Task Open (
            QuarantineInfoHandler quarantineInfoHandler,
            FilePath openPath,
            params AgentType [] platformTargets)
        {
            if (quarantineInfoHandler == null)
                throw new ArgumentNullException (nameof (quarantineInfoHandler));

            SaveOptions = WorkbookSaveOptions.None;
            LogicalPath = openPath;

            QuarantineInfo quarantineInfo = null;

            var isDirectory = openPath.DirectoryExists;
            if (!isDirectory) {
                try {
                    quarantineInfo = ClientApp
                        .SharedInstance
                        .FileSystem
                        .GetQuarantineInfo (openPath);
                } catch (Exception e) {
                    Log.Error (TAG, $"unable to retrieve quarantine info from {openPath}", e);
                }
            }

            Open (openPath, openPath.DirectoryExists, platformTargets);

            if (quarantineInfo != null && await quarantineInfoHandler (quarantineInfo)) {
                try {
                    ClientApp
                        .SharedInstance
                        .FileSystem
                        .StripQuarantineInfo (quarantineInfo.RepresentedFile);
                } catch (Exception e) {
                    Log.Error (TAG, $"unable to strip quarantine info from {openPath}", e);
                }
            }

            // our title is based on the first page
            NotifyPropertyChanged (nameof (Title));
        }

        void Open (FilePath openPath, bool isDirectory, params AgentType [] platformTargets)
        {
            pages.Clear ();

            var indexPage = new WorkbookPage (this, platformTargets);

            if (openPath.IsNull) {
                WorkingPath = FilePath.Empty;
                AddPage (indexPage);
                return;
            }

            FilePath readPath;

            if (isDirectory) {
                indexPage.Path = indexPageFileName;
                readPath = openPath.Combine (indexPage.Path);
                openedFromDirectory = true;
            } else {
                // If we are opening a file within a .workbook directory somewhere,
                // actually open that directory. This is to mostly address issues
                // on Windows where you cannot open a directory via Explorer.
                //
                // FIXME: The file within the workbook directory is intentionally
                // ignored for now, so this will effectively open index.workbook,
                // hitting the 'isDirectory' path above via the recursive Open call.
                // In reality for now it does not matter since we do not support
                // workbooks with multiple pages.
                for (var parentPath = openPath.ParentDirectory;
                    !parentPath.IsNull;
                    parentPath = parentPath.ParentDirectory) {
                    if (parentPath.DirectoryExists && parentPath.Extension == dottedExtension) {
                        LogicalPath = parentPath;
                        Open (parentPath, true, platformTargets);
                        return;
                    }
                }

                indexPage.Path = openPath.Name;
                readPath = openPath;
            }

            using (var stream = new FileStream (
                readPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read)) {
                if (!isDirectory) {
                    if (ZipArchiveExtensions.SmellHeader (stream)) {
                        OpenZip (stream, platformTargets);
                        return;
                    }

                    // seek back to start since we just checked for a Zip header
                    stream.Seek (0, SeekOrigin.Begin);
                }

                indexPage.Read (new StreamReader (stream));
            }

            AddPage (indexPage);
            WorkingPath = openPath;
        }

        void OpenZip (FileStream stream, AgentType [] platformTargets)
        {
            var extractPath = privateWorkingRootPath.GetTempFileName (extension);

            var zipArchive = new ZipArchive (
                stream,
                ZipArchiveMode.Read,
                true,
                Utf8.Encoding);

            zipArchive.Extract (extractPath, preserveRootDirectory: false);

            Open (extractPath, true, platformTargets);

            SaveOptions |= WorkbookSaveOptions.Archive;
        }

        FilePath GetTempZipArchivePath ()
            => ClientApp
                .SharedInstance
                .FileSystem
                .GetTempDirectory ("archived-packages")
                .GetTempFileName (extension + ".zip");

        sealed class SaveOperation : IWorkbookSaveOperation
        {
            public Dictionary<WorkbookPage, List<FilePath>> AllDependencies;
            public WorkbookPage OnlyPage;
            public bool OnlyPageHasDependencies;

            public WorkbookSaveOptions SupportedOptions {
                get {
                    if (OnlyPage == null || OnlyPageHasDependencies)
                        return WorkbookSaveOptions.Archive;

                    return WorkbookSaveOptions.None;
                }
            }

            public WorkbookSaveOptions Options { get; set; }
            public FilePath Destination { get; set; }
        }

        public IWorkbookSaveOperation CreateSaveOperation ()
        {
            var dependencies = new WorkbookDependencyCollector ().Visit (this);
            var onlyPage = dependencies.SingleOrDefault ();

            return new SaveOperation {
                Destination = logicalPath,
                AllDependencies = dependencies,
                OnlyPage = onlyPage.Key,
                OnlyPageHasDependencies = onlyPage.Key != null && onlyPage.Value.Count > 0,
                Options = SaveOptions
            };
        }

        public void Save (IWorkbookSaveOperation operation)
        {
            var saveOperation = operation as SaveOperation;
            if (saveOperation == null)
                throw new ArgumentNullException (nameof (operation));

            LogicalPath = operation.Destination;
            SaveOptions = WorkbookSaveOptions.None;

            if (IndexPage != null && IndexPage.IsUntitled)
                IndexPage.Title = LogicalPath.NameWithoutExtension;

            if (saveOperation.OnlyPage != null &&
                !saveOperation.OnlyPageHasDependencies &&
                !openedFromDirectory) {
                // The workbook package has only a single page with no relative
                // dependencies. If the original path was a workbook directory,
                // keep that format. If we originally opened from a workbook
                // directory and our target save path is not a directory, keep
                // that format. Otherwise, save the page as a single file.
                var writePath = logicalPath.DirectoryExists
                    ? logicalPath.Combine (saveOperation.OnlyPage.Path)
                    : logicalPath;

                using (var writer = new StreamWriter (writePath))
                    saveOperation.OnlyPage.Write (writer, Packages);

                WorkingPath = logicalPath;
                return;
            }

            var sourceBasePath = WorkingBasePath;

            if (saveOperation.OnlyPage != null)
                saveOperation.OnlyPage.Path = indexPageFileName;

            if (logicalPath.FileExists)
                File.Delete (logicalPath);

            logicalPath.CreateDirectory ();

            if (WorkingPath.DirectoryExists && WorkingPath.Extension == dottedExtension)
                CopyDirectoryContents (WorkingPath, logicalPath);

            foreach (var page in saveOperation.AllDependencies) {
                using (var writer = new StreamWriter (logicalPath.Combine (page.Key.Path)))
                    page.Key.Write (writer, Packages);

                foreach (var dep in page.Value) {
                    var sourcePath = sourceBasePath.Combine (dep);
                    if (!sourcePath.FileExists)
                        continue;

                    // FIXME: if the dep is not a child of the baseSourcePath this
                    // copy will result in the file not being located in the actual
                    // workbook package (e.g. the dep is something like '../foo.jpg').
                    // Adjust the dest path so that it will be copied into the package,
                    // but the workbook content itself will be broken until we can
                    // actually fix this up in the markdown.
                    var destPath = logicalPath.Combine (dep);
                    if (destPath.IsChildOfDirectory (logicalPath) && destPath != sourcePath) {
                        destPath.ParentDirectory.CreateDirectory ();
                        File.Copy (sourcePath, destPath, true);
                    }
                }
            }

            if (saveOperation.Options.HasFlag (WorkbookSaveOptions.Archive))
                ArchiveDirectory (logicalPath);

            // For overwrite saves of archives, WorkingPath should continue
            // to point to the extracted directory in temp
            if (!saveOperation.Options.HasFlag (WorkbookSaveOptions.Archive) ||
                WorkingPath == null ||
                !WorkingPath.Exists)
                WorkingPath = logicalPath;
        }

        void CopyDirectoryContents (FilePath sourceDirectory, FilePath destDirectory)
        {
            if (sourceDirectory == destDirectory)
                return;

            foreach (var sourcePath in sourceDirectory
                     .EnumerateFiles ("*", SearchOption.AllDirectories)
                     .ToArray ()) {
                var targetPath = sourcePath.GetRelativePath (sourceDirectory);
                targetPath = destDirectory.Combine (targetPath);
                targetPath.ParentDirectory.CreateDirectory ();
                File.Copy (sourcePath, targetPath, true);
            }
        }

        void ArchiveDirectory (FilePath directory)
        {
            SaveOptions |= WorkbookSaveOptions.Archive;

            var archivePath = GetTempZipArchivePath ();

            using (var stream = new FileStream (
                archivePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None))
            using (var archive = new ZipArchive (
                stream,
                ZipArchiveMode.Create,
                true,
                Utf8.Encoding)) {
                foreach (var entryPath in directory.EnumerateFiles (
                    searchOption: SearchOption.AllDirectories)) {
                    var entry = archive.CreateEntry (entryPath.GetRelativePath (directory));
                    entry.LastWriteTime = File.GetLastWriteTime (entryPath);
                    using (var entrySourceStream = entryPath.OpenRead ())
                    using (var entryDestStream = entry.Open ())
                        entrySourceStream.CopyTo (entryDestStream);
                }
            }

            Directory.Delete (directory, true);
            File.Move (archivePath, directory);
        }
    }
}