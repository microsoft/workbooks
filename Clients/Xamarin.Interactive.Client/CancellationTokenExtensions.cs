//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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