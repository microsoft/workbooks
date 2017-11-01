//
// XipErrorMessageException.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

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