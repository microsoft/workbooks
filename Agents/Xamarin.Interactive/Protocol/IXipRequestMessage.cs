//
// IXipRequestMessage.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive.Protocol
{
	interface IXipRequestMessage
	{
		Guid MessageId { get; }
	}

	interface IXipRequestMessage<T> : IXipRequestMessage
	{
		void Handle (T context, Action<object> responseWriter);
	}
}