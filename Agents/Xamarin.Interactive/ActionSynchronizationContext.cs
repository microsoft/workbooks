//
// ActionSynchronizationContext.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Threading;

namespace Xamarin.Interactive
{
	sealed class ActionSynchronizationContext : SynchronizationContext
	{
		readonly Action<Action> postHandler;
		readonly Action<Action> sendHandler;

		public ActionSynchronizationContext (Action<Action> postHandler, Action<Action> sendHandler)
		{
			this.postHandler = postHandler;
			this.sendHandler = sendHandler;
		}

		public override void Post (SendOrPostCallback d, object state)
		{
			if (postHandler == null)
				base.Post (d, state);
			else
				postHandler (() => d (state));
		}

		public override void Send (SendOrPostCallback d, object state)
		{
			if (sendHandler == null)
				base.Send (d, state);
			else
				sendHandler (() => d (state));
		}

		public override SynchronizationContext CreateCopy ()
			=> new ActionSynchronizationContext (postHandler, sendHandler);
	}
}