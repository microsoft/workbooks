//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Microsoft.CodeAnalysis;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using Xamarin.Interactive.Client.Monaco;
using Xamarin.Interactive.Client.Web.Hosting;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Completion;
using Xamarin.Interactive.CodeAnalysis.Hover;
using Xamarin.Interactive.CodeAnalysis.SignatureHelp;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Client.Web
{
    sealed partial class InteractiveSessionHub : Hub
    {
        readonly IServiceProvider serviceProvider;

        public InteractiveSessionHub (IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public override Task OnConnectedAsync ()
        {
            serviceProvider
                .GetInteractiveSessionHubManager ()
                .OnConnectedAsync (Context.Connection);

            return base.OnConnectedAsync ();
        }

        public override Task OnDisconnectedAsync (Exception exception)
        {
            serviceProvider
                .GetInteractiveSessionHubManager ()
                .OnDisconnectedAsync (Context.Connection);

            return base.OnDisconnectedAsync (exception);
        }

        public IEnumerable<WorkbookAppInstallation> GetAvailableWorkbookTargets ()
            => WorkbookAppInstallation.All;

        public Task OpenSession (string sessionUri)
        {
            if (!ClientSessionUri.TryParse (sessionUri, out var uri))
                throw new Exception ("Invalid client session URI");

            var connectionId = Context.ConnectionId;

            MainThread.Post (() => InitializeClientSessionAsync (connectionId, new ClientSession (uri)).Forget ());

            return Task.CompletedTask;
        }

        async Task InitializeClientSessionAsync (string connectionId, ClientSession session)
        {
            session.InitializeViewControllers (new WebClientSessionViewControllers (connectionId, serviceProvider));
            await session.InitializeAsync ();
            await session.EnsureAgentConnectionAsync ();

            serviceProvider
                .GetInteractiveSessionHubManager ()
                .BindClientSession (connectionId, session);
        }

        public async Task<string> InsertCodeCell (
            string initialBuffer,
            string relativeToCodeCellId,
            bool insertBefore)
        {
            var sessionState = serviceProvider
                .GetInteractiveSessionHubManager ()
                .GetSession (Context.ConnectionId);

            var codeCellState = await sessionState
                .EvaluationService
                .InsertCodeCellAsync (
                    initialBuffer,
                    relativeToCodeCellId,
                    insertBefore,
                    Context.Connection.ConnectionAbortedToken);

            return codeCellState.Id;
        }

        public async Task<CodeCellStatus> UpdateCodeCell (
            string codeCellId,
            string updatedBuffer)
        {
            var sessionState = serviceProvider
                .GetInteractiveSessionHubManager ()
                .GetSession (Context.ConnectionId);

            var cellState = await sessionState
                .EvaluationService
                .UpdateCodeCellAsync (
                    codeCellId,
                    updatedBuffer,
                    Context.Connection.ConnectionAbortedToken);

            var workspace = sessionState
                    .ClientSession
                    .CompilationWorkspace;
            var documentId = cellState.Id.ToDocumentId ();

            var diagnostics = await workspace.GetSubmissionCompilationDiagnosticsAsync (
                documentId, Context.Connection.ConnectionAbortedToken);

            return new CodeCellStatus {
                IsSubmissionComplete = sessionState
                    .ClientSession
                    .CompilationWorkspace
                    .IsDocumentSubmissionComplete (documentId),
                Diagnostics = diagnostics
                    .Where (d => d.Severity == DiagnosticSeverity.Error)
                    .Select (d => new MonacoModelDeltaDecoration (d))
                    .ToArray ()
            };
        }

        public Task Evaluate (string targetCodeCellId, bool evaluateAll)
        {
            var sessionState = serviceProvider
                .GetInteractiveSessionHubManager ()
                .GetSession (Context.ConnectionId);

            return sessionState.EvaluationService.EvaluateAsync (
                targetCodeCellId,
                evaluateAll,
                Context.Connection.ConnectionAbortedToken);
        }

        // TODO: Probably want package source URL, too
        public async Task<List<string>> InstallPackage (string id, string version)
        {
            var sessionState = serviceProvider
                .GetInteractiveSessionHubManager ()
                .GetSession (Context.ConnectionId);

            await sessionState.ClientSession.InstallPackageAsync (
                new PackageViewModel (new PackageIdentity (
                    id,
                    new NuGetVersion (version))),
                Context.Connection.ConnectionAbortedToken);

            // TODO: Probably want to return a more detailed VM, not just IDs
            return sessionState
                .ClientSession
                .Workbook
                .Packages
                .InstalledPackages
                .Select (p => p.Identity.Id)
                .ToList ();
        }

        public async Task<List<MonacoCompletionItem>> ProvideCompletions (
            string targetCodeCellId,
            int lineNumber,
            int column)
        {
            var sessionState = serviceProvider
                    .GetInteractiveSessionHubManager ()
                    .GetSession (Context.ConnectionId);

            if (sessionState.CompletionController == null)
                sessionState.CompletionController = new CompletionController (
                    sessionState.ClientSession.CompilationWorkspace);

            var codeCells = await sessionState.EvaluationService.GetAllCodeCellsAsync (
                Context.Connection.ConnectionAbortedToken);
            var targetCodeCellState = codeCells.FirstOrDefault (c => c.Id == targetCodeCellId);

            var completionItems = await sessionState.CompletionController.ProvideFilteredCompletionItemsAsync (
                targetCodeCellState.Buffer.CurrentText,
                new Microsoft.CodeAnalysis.Text.LinePosition (lineNumber - 1, column - 1),
                Context.Connection.ConnectionAbortedToken);

            return completionItems
                .Select (i => new MonacoCompletionItem (i))
                .ToList ();
        }

        public async Task<MonacoHover> ProvideHover (
            string targetCodeCellId,
            int lineNumber,
            int column)
        {
            var sessionState = serviceProvider
                    .GetInteractiveSessionHubManager ()
                    .GetSession (Context.ConnectionId);

            if (sessionState.HoverController == null)
                sessionState.HoverController = new HoverController (
                    sessionState.ClientSession.CompilationWorkspace);

            var codeCells = await sessionState.EvaluationService.GetAllCodeCellsAsync (
                Context.Connection.ConnectionAbortedToken);
            var targetCodeCellState = codeCells.FirstOrDefault (c => c.Id == targetCodeCellId);

            var hover = await sessionState.HoverController.ProvideHoverAsync (
                targetCodeCellState.Buffer.CurrentText,
                new Microsoft.CodeAnalysis.Text.LinePosition (lineNumber - 1, column - 1),
                Context.Connection.ConnectionAbortedToken);

            return new MonacoHover (hover);
        }

        public async Task<SignatureHelpViewModel> ProvideSignatureHelp (
            string targetCodeCellId,
            int lineNumber,
            int column)
        {
            var sessionState = serviceProvider
                    .GetInteractiveSessionHubManager ()
                    .GetSession (Context.ConnectionId);

            if (sessionState.SignatureHelpController == null)
                sessionState.SignatureHelpController = new SignatureHelpController (
                    sessionState.ClientSession.CompilationWorkspace);

            var codeCells = await sessionState.EvaluationService.GetAllCodeCellsAsync (
                Context.Connection.ConnectionAbortedToken);
            var targetCodeCellState = codeCells.FirstOrDefault (c => c.Id == targetCodeCellId);

            var signatureHelp = await sessionState.SignatureHelpController.ComputeSignatureHelpAsync (
                targetCodeCellState.Buffer.CurrentText,
                new Microsoft.CodeAnalysis.Text.LinePosition (lineNumber - 1, column - 1),
                Context.Connection.ConnectionAbortedToken);

            return signatureHelp;
        }
    }
}