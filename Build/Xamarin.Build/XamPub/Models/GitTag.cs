//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.XamPub.Models
{
    sealed class GitTag
    {
        [JsonProperty ("name")]
        public string Name { get; set; }

        [JsonProperty ("message")]
        public string Message { get; set; }
    }
}