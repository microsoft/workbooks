//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis.Text;

namespace Xamarin.Interactive.Client.Monaco
{
    class MonacoRange
    {
        public int StartLineNumber { get; }

        public int StartColumn { get; }

        public int EndLineNumber { get; }

        public int EndColumn { get; }

        public MonacoRange (LinePositionSpan span)
        {
            StartLineNumber = span.Start.Line + 1;
            StartColumn = span.Start.Character + 1;
            EndLineNumber = span.End.Line + 1;
            EndColumn = span.End.Character + 1;
        }
    }
}
