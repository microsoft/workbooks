//
// CancellationTokenExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.Threading;

namespace Xamarin.Interactive
{
	static class CancellationTokenExtensions
	{
		public static CancellationToken LinkWith (this CancellationToken a, CancellationToken b)
			=> b == default (CancellationToken)
				? a
				: CancellationTokenSource.CreateLinkedTokenSource (a, b).Token;
	}
}