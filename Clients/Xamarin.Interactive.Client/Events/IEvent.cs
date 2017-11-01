//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Events
{
    public interface IEvent
    {
        object Source { get; }
        DateTime Timestamp { get; }
    }
}