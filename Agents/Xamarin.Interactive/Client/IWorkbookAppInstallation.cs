// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client
{
    interface IWorkbookAppInstallation
    {
        string Id { get; }

        /// <summary>
        /// The "flavor" is the top-level grouping of workbook apps.
        /// The short form would be "iOS", for example.
        /// </summary>
        string Flavor { get; }

        /// <summary>
        /// Icon name.
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// IDs of any optional features the workbook app supports.
        /// </summary>
        string [] OptionalFeatures { get; }

        /// <summary>
        /// SDK information for this application.
        /// </summary>
        Sdk Sdk { get; }

        /// <summary>
        /// Path to the actual application.
        /// </summary>
        string AppPath { get; }

        Task<IAgentTicket> RequestAgentTicketAsync (
            ClientSessionUri clientSessionUri,
            IMessageService messageService,
            Action disconnectedHandler,
            CancellationToken cancellationToken = default (CancellationToken));
    }
}
