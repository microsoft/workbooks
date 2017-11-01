//
// AgentStartOptions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.Core
{
	public class AgentStartOptions
	{
		internal ClientSessionKind? ClientSessionKind { get; set; }
		public ushort? Port { get; set; }
	}
}