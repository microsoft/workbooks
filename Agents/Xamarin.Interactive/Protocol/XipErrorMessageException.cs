//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Protocol
{
    sealed class XipErrorMessageException : Exception
    {
        public XipErrorMessage XipErrorMessage { get; private set; }

        public XipErrorMessageException (XipErrorMessage message)
            : base (message.Message ?? "unhandled exception")
        {
            XipErrorMessage = message;
        }
    }
}