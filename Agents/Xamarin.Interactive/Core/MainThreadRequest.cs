//
// MainThreadRequest.cs
//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	abstract class MainThreadRequest<TResponseMessage> : IXipRequestMessage<Agent>
		where TResponseMessage : class
	{
		public Guid MessageId { get; } = Guid.NewGuid ();

		protected virtual bool CanReturnNull => false;

		protected abstract Task<TResponseMessage> HandleAsync (Agent agent);

		public void Handle (Agent agent, Action<object> responseWriter)
		{
			object result = null;

			Task.Factory.StartNew (
				async () => {
					try {
						result = await HandleAsync (agent);
						if (result == null && !CanReturnNull)
							throw new Exception ($"{GetType ()}.Handle(Agent) returned null");
					} catch (Exception e) {
						result = new XipErrorMessage {
							Message = $"{GetType ()}.Handle(Agent) threw an exception",
							Exception = ExceptionNode.Create (e)
						};
					}
				},
				CancellationToken.None,
				TaskCreationOptions.DenyChildAttach,
				MainThread.TaskScheduler).Unwrap ().Wait ();

			responseWriter (result);
		}
	}
}