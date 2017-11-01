//
// JsonPayload.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Serialization
{
    [Serializable]
    struct JsonPayload
    {
        readonly string data;

        JsonPayload (string data)
        {
            this.data = data;
        }

        public override string ToString () => data;

        public static implicit operator string (JsonPayload payload) => payload.data;
        public static implicit operator JsonPayload (string payload) => new JsonPayload (payload);
    }
}