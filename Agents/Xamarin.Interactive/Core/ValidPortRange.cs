//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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