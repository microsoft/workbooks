//
// IXipResponseMessage.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Protocol
{
    interface IXipResponseMessage
    {
        Guid RequestId { get; }
    }
}