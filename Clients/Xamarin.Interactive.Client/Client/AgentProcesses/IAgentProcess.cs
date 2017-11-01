//
// IAgentProcess.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client.AgentProcesses
{
	interface IAgentProcess
	{
		event EventHandler UnexpectedlyTerminated;

		WorkbookAppInstallation WorkbookApp { get; }

		Task StartAgentProcessAsync (
			IdentifyAgentRequest identifyAgentRequest,
			IMessageService messageService,
			CancellationToken cancellationToken);

		Task TerminateAgentProcessAsync ();
	}
}