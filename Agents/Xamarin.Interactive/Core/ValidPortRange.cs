//
// ValidPortRange.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive
{
	static class ValidPortRange
	{
		// IANA recommendation for private ports
		public const int Minimum = 49152;
		public const int Maximum = 65535;

		public static bool IsValid (int port)
			=> port >= Minimum && port <= Maximum;

		public static void Assert (int port)
		{
			if (!IsValid (port))
				throw new ArgumentOutOfRangeException (
					nameof (port),
					$"{Minimum} <= {nameof (port)} ({port}) <= {Maximum}");
		}

		public static int GetRandom ()
			=> new Random ().Next (Minimum, Maximum + 1);
	}
}