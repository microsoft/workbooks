// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Microsoft.Extensions.Caching.Memory;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Session;

namespace Xamarin.Interactive.Client.Web.Hubs
{
    /// <summary>
    /// SignalR proxy for InteractiveSession.
    /// </summary>
    /// <remarks>
    /// Remember that SignalR Hub has the same lifecycle semantics as MVC
    /// controllers: a new instance will be created for each hub method
    /// invocation (including OnConnectedAsync and OnDisconnectedAsync).
    ///
    /// TL;DR: Do not store state in this class!
    /// </remarks>
    sealed class InteractiveSessionHub : Hub
    {
        readonly IMemoryCache memoryCache;

        public InteractiveSessionHub (IMemoryCache memoryCache)
            => this.memoryCache = memoryCache;

        public override Task OnConnectedAsync ()
        {
            memoryCache.GetOrCreate (
                Context.ConnectionId,
                e => new InteractiveSession ());

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync (Exception exception)
        {
            if (memoryCache.TryGetValue (
                Context.ConnectionId,
                out InteractiveSession session))
                session.Dispose ();

            memoryCache.Remove (Context.ConnectionId);

            return Task.CompletedTask;
        }

        InteractiveSession GetSession ()
            => memoryCache.Get<InteractiveSession> (Context.ConnectionId);

        public IEnumerable<WorkbookAppInstallation> GetAvailableWorkbookTargets ()
            => WorkbookAppInstallation.All;

        public IObservable<InteractiveSessionEvent> ObserveSessionEvents ()
            => GetSession ().Events;

        public Task InitializeSession (InteractiveSessionDescription sessionDescription)
            => GetSession ().InitializeAsync (
                sessionDescription,
                Context.Connection.ConnectionAbortedToken);

        public Task<CodeCellId> InsertCodeCell (
            string initialBuffer,
            string relativeToCodeCellId,
            bool insertBefore)
            => GetSession ().EvaluationService.InsertCodeCellAsync (
                initialBuffer,
                relativeToCodeCellId,
                insertBefore,
                Context.Connection.ConnectionAbortedToken);

        public Task<CodeCellUpdatedEvent> UpdateCodeCell (
            string codeCellId,
            string updatedBuffer)
            => GetSession ().EvaluationService.UpdateCodeCellAsync (
                codeCellId,
                updatedBuffer,
                Context.Connection.ConnectionAbortedToken);

        public Task Evaluate (string targetCodeCellId, bool evaluateAll)
            => GetSession ().EvaluationService.EvaluateAsync (
                targetCodeCellId,
                evaluateAll,
                Context.Connection.ConnectionAbortedToken);

        public Task<IInteractiveObject> Interact (string handle)
            => GetSession().EvaluationService.InteractAsync(
                long.Parse (handle),
                cancellationToken: Context.Connection.ConnectionAbortedToken);

        public Task<Hover> GetHover (
            string codeCellId,
            Position position)
            => GetSession ().WorkspaceService.GetHoverAsync (
                codeCellId,
                position,
                Context.Connection.ConnectionAbortedToken);

        public Task<IEnumerable<CompletionItem>> GetCompletions (
            CodeCellId codeCellId,
            Position position)
            => GetSession ().WorkspaceService.GetCompletionsAsync (
                codeCellId,
                position,
                Context.Connection.ConnectionAbortedToken);

        public Task<SignatureHelp> GetSignatureHelp (
            CodeCellId codeCellId,
            Position position)
            => GetSession ().WorkspaceService.GetSignatureHelpAsync (
                codeCellId,
                position,
                Context.Connection.ConnectionAbortedToken);

        public async Task<IReadOnlyList<InteractivePackageDescription>> InstallPackages (
            IReadOnlyList<InteractivePackageDescription> packages)
        {
            var packageManagerService = GetSession ().PackageManagerService;
            await packageManagerService.InstallAsync (
                packages,
                Context.Connection.ConnectionAbortedToken);
            return packageManagerService.GetInstalledPackages ();
        }

        public async Task<IReadOnlyList<InteractivePackageDescription>> RestorePackages (
            IReadOnlyList<InteractivePackageDescription> packages)
        {
            var packageManagerService = GetSession ().PackageManagerService;
            await packageManagerService.RestoreAsync (
                packages,
                Context.Connection.ConnectionAbortedToken);
            return packageManagerService.GetInstalledPackages ();
        }
    }
}
