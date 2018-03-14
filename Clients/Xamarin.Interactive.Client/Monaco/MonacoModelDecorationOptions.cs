//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Client.Monaco
{
    class MonacoModelDecorationOptions
    {
        public string InlineClassName { get; }

        public string HoverMessage { get; }

        public MonacoModelDecorationOptions (string message, string className)
        {
            HoverMessage = message;
            InlineClassName = className;
        }
    }
}
