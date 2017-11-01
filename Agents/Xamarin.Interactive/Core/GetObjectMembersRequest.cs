//
// GetPropertiesRequest.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class GetObjectMembersRequest : MainThreadRequest<InteractiveObject>
	{
		protected override bool CanReturnNull => true;

		public long ViewHandle { get; set; }

		protected override Task<InteractiveObject> HandleAsync (Agent agent)
		{
			var members = agent.RepresentationManager.PrepareInteractiveObject (
				ObjectCache.Shared.GetObject (ViewHandle));
			members?.Interact (new InteractiveObject.ReadAllMembersInteractMessage ());
			return Task.FromResult (members);
		}
	}
}