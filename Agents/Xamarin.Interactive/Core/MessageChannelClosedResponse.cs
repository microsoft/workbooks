//
// MessageChannelClosedResponse.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Protocol;

namespace Xamarin.Interactive.Core
{
	sealed class MessageChannelClosedResponse : Exception, IXipResponseMessage
	{
		public Guid RequestId { get; }

		public MessageChannelClosedResponse (Guid requestId)
			=> RequestId = requestId;
	}
}