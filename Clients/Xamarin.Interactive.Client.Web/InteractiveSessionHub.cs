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

using Xamarin.Interactive.Client.Web.Hosting;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Completion;

namespace Xamarin.Interactive.Client.Web
{
    sealed class InteractiveSessionHub : Hub
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

        public Task UpdateCodeCell (
            string codeCellId,
            string updatedBuffer)
            => serviceProvider
                .GetInteractiveSessionHubManager ()
                .GetSession (Context.ConnectionId)
                .EvaluationService
                .UpdateCodeCellAsync (
                    codeCellId,
                    updatedBuffer,
                    Context.Connection.ConnectionAbortedToken);

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

        public async Task<List<MonacoCompletionItem>> ProvideCompletions (
            string targetCodeCellId,
            int lineNumber,
            int column)
        {
            // TODO: Figure out how much we need to mess with task scheduling. See TODO at end of ModelComputation
            async Task<List<MonacoCompletionItem>> InnerProvideCompletionsAsync () {
                var sessionState = serviceProvider
                    .GetInteractiveSessionHubManager ()
                    .GetSession (Context.ConnectionId);

                if (sessionState.CompletionController == null)
                    sessionState.CompletionController = new CompletionController (sessionState.ClientSession.CompilationWorkspace);

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

            return await InnerProvideCompletionsAsync ();
        }
    }

    // TODO: Review accessibility. Move.
    public class MonacoCompletionItem
    {
        public string Label { get; }

        public string InsertText { get; }

        public string Detail { get; }

        // Corresponds to Monaco's CompletionItemKind enum
        public int Kind { get; }

        internal MonacoCompletionItem (CompletionItemViewModel itemViewModel)
        {
            Label = itemViewModel.DisplayText;
            // TODO: Can we tell the serializer to exclude insertText and detail if null? Right now I'm having to
            //       loop through the list on the client side and replace null with undefined (Monaco breaks otherwise).
            InsertText = itemViewModel.InsertionText;
            Detail = itemViewModel.ItemDetail;
            Kind = 1; // TODO: How much of MonacoExtensions should move into XIC?
        }
    }
}