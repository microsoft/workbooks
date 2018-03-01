//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.Client.Monaco
{
    class MonacoModelDeltaDecoration
    {
        public MonacoRange Range { get; }

        public MonacoModelDecorationOptions Options { get; }

        public MonacoModelDeltaDecoration (Diagnostic diagnostic)
        {
            Range = new MonacoRange (diagnostic.Location.GetLineSpan ().Span);
            Options = new MonacoModelDecorationOptions (
                diagnostic.GetMessage (), "xi-diagnostic");
        }
    }
}
