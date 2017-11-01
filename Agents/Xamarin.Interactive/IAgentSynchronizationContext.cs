//
// IAgentSynchronizationContext.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Threading;

namespace Xamarin.Interactive
{
	public interface IAgentSynchronizationContext
	{
		SynchronizationContext PushContext (Action<Action> postHandler, Action<Action> sendHandler = null);
		SynchronizationContext PushContext (SynchronizationContext context);
		SynchronizationContext PeekContext ();
		SynchronizationContext PopContext ();
	}
}