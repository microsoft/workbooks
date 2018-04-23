//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
    public static class WorkspaceServiceFactory
    {
        static ImmutableDictionary<string, IWorkspaceServiceActivator> activators
            = Directory
                .EnumerateFiles (
                    Path.GetDirectoryName (typeof (WorkspaceServiceFactory).Assembly.Location),
                    "Xamarin.Interactive.CodeAnalysis.*.dll",
                    SearchOption.TopDirectoryOnly)
                .Select (Assembly.LoadFrom)
                .SelectMany (assembly => assembly.GetCustomAttributes<WorkspaceServiceAttribute> ())
                .ToImmutableDictionary (
                    attribute => attribute.LanguageName,
                    attribute => {
                        if (!typeof (IWorkspaceServiceActivator)
                            .IsAssignableFrom (attribute.WorkspaceServiceActivatorType))
                            throw new Exception (
                                $"{attribute.WorkspaceServiceActivatorType.FullName} must implement " +
                                $"{typeof (IWorkspaceServiceActivator).FullName} in order to provide " +
                                $"support for language '{attribute.LanguageName}'");
                        return (IWorkspaceServiceActivator)Activator.CreateInstance (
                            attribute.WorkspaceServiceActivatorType);
                    });

        public static async Task<IWorkspaceService> CreateWorkspaceServiceAsync (
            LanguageDescription languageDescription,
            WorkspaceConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            if (!activators.TryGetValue (languageDescription.Name, out var activator))
                throw new Exception (
                    $"Unable to resolve a workspace service activator " +
                    $"for language '{languageDescription.Name}'");

            return await activator.CreateNew (
                languageDescription,
                configuration,
                cancellationToken).ConfigureAwait (false);
        }
    }
}