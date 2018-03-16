//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis
{
    public static class WorkspaceServiceFactory
    {
        static WorkspaceServiceFactory ()
        {
            RegisterProvider ("csharp", (language, configuration, cancellationToken)
                => Task.FromResult<IWorkspaceService> (new RoslynCompilationWorkspace (configuration)));
        }

        internal delegate Task<IWorkspaceService> ServiceProvider (
            LanguageDescription languageDescription,
            WorkspaceConfiguration configuration,
            CancellationToken cancellationToken);

        static ImmutableDictionary<string, ServiceProvider> providers
            = ImmutableDictionary<string, ServiceProvider>.Empty;

        internal static void RegisterProvider (string languageName, ServiceProvider provider)
            => providers = providers.Add (
                languageName ?? throw new ArgumentNullException (nameof (provider)),
                provider ?? throw new ArgumentNullException (nameof (provider)));

        internal static async Task<IWorkspaceService> CreateWorkspaceServiceAsync (
            LanguageDescription languageDescription,
            WorkspaceConfiguration configuration,
            CancellationToken cancellationToken)
        {
            if (!providers.TryGetValue (languageDescription.Name, out var provider))
                throw new Exception ($"Unable to resolve a workspace service for language '{languageDescription.Name}'");

            return await provider (
                languageDescription,
                configuration,
                cancellationToken).ConfigureAwait (false);
        }
    }
}