// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Session
{
    public struct InteractiveSessionEvent
    {
        public InteractiveSessionEventKind Kind { get; }
        public object Data { get; }

        [JsonConstructor]
        public InteractiveSessionEvent (InteractiveSessionEventKind kind, object data = null)
        {
            Kind = kind;
            Data = data;
        }
    }
}