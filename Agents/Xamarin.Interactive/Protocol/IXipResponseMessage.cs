//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Protocol
{
    interface IXipResponseMessage
    {
        Guid RequestId { get; }
    }
}