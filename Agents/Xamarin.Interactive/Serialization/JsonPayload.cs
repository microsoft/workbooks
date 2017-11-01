//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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