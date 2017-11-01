//
// InteractivePackageProjectContext.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Globalization;
using System.Xml.Linq;

using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectManagement;

namespace Xamarin.Interactive.NuGet
{
    sealed class InteractivePackageProjectContext : INuGetProjectContext
    {
        readonly ILogger logger;

        public InteractivePackageProjectContext (ILogger logger)
        {
            this.logger = logger;
            PackageExtractionContext = new PackageExtractionContext (logger) {
                PackageSaveMode = PackageSaveMode.Defaultv3,
            };
        }

        public NuGetActionType ActionType { get; set; }

        public ExecutionContext ExecutionContext => null;

        public XDocument OriginalPackagesConfig { get; set; }

        public PackageExtractionContext PackageExtractionContext { get; set; }

        public ISourceControlManagerProvider SourceControlManagerProvider => null;

        public TelemetryServiceHelper TelemetryService { get; set; }

        public void Log (MessageLevel level, string message, params object [] args)
        {
            // Copied from NuGet.CommandLine.ConsoleProjectContext:

            if (args.Length > 0)
                message = string.Format (CultureInfo.CurrentCulture, message, args);

            switch (level) {
            case MessageLevel.Debug:
                logger.LogDebug (message);
                break;

            case MessageLevel.Info:
                logger.LogMinimal (message);
                break;

            case MessageLevel.Warning:
                logger.LogWarning (message);
                break;

            case MessageLevel.Error:
                logger.LogError (message);
                break;
            }
        }

        public void ReportError (string message)
            => logger.LogError (message); // TODO: Surface to UI? When is this called?

        public FileConflictAction ResolveFileConflict (string message)
            => FileConflictAction.IgnoreAll; // TODO
    }
}
