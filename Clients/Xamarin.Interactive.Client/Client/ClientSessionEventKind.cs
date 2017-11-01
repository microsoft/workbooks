//
// ClientSessionEventKind.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive
{
	enum ClientSessionEventKind
	{
		/// <summary>
		/// Will be raised once per subscription indicating that the session is available for use.
		/// </summary>
		SessionAvailable,

		/// <summary>
		/// Will be raised when the session title has changed, such as when a workbook is saved.
		/// </summary>
		SessionTitleUpdated,

		/// <summary>
		/// Can be raised any number of times per subscription, indicating that a connection
		/// to the agent associated with the session has been made and that ClientSession.Agent.Api
		/// is usable.
		/// </summary>
		AgentConnected,

		/// <summary>
		/// Can raised any number of times per subscription, indicating that agent-side features
		/// may have changed, such as new available view hierarchies.
		/// </summary>
		///
		AgentFeaturesUpdated,

		/// <summary>
		/// Can be raised any number of times per subscription, indicating that a connection
		/// to the agent associated with the session has been lost and that ClientSession.Agent.Api
		/// is not available.
		/// </summary>
		AgentDisconnected,

		/// <summary>
		/// Can be raised any number of times per subscription, indicating that the compilation
		/// workspace is ready for use. Will always be invoked after <see cref="AgentConnected"/>.
		/// </summary>
		CompilationWorkspaceAvailable
	}
}