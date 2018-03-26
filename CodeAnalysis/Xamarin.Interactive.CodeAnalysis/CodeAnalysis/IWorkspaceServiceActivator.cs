// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.CodeAnalysis
{
    public interface IWorkspaceServiceActivator
    {
        Task<IWorkspaceService> CreateNew (
            LanguageDescription languageDescription,
            WorkspaceConfiguration configuration,
            CancellationToken cancellationToken);
    }
}