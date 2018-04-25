// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class SetObjectMemberResponse
    {
        public bool Success { get; }
        public InteractiveObject UpdatedValue { get; }

        [JsonConstructor]
        public SetObjectMemberResponse (
            bool success,
            InteractiveObject updatedValue)
        {
            Success = success;
            UpdatedValue = updatedValue;
        }
    }
}