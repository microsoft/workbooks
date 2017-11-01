//
// ExecFlags.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.ProcessControl
{
	[Flags]
	public enum ExecFlags
	{
		None = 0 << 0,
		RedirectStdin = 1 << 0,
		RedirectStdout = 1 << 1,
		RedirectStderr = 1 << 2,
		Elevate = 1 << 3,
		OutputOnSynchronizationContext = 1 << 4,

		Default = OutputOnSynchronizationContext
	}
}