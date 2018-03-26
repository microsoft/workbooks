//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.XamPub.Models
{
    sealed class ReleaseInfo
    {
        [JsonProperty ("name")]
        [JsonRequired]
        public string Name { get; set; }

        [JsonProperty ("alias")]
        [JsonRequired]
        public string Alias { get; set; }

        [JsonProperty ("region")]
        [JsonRequired]
        public string Region { get; set; }

        [JsonProperty ("description")]
        [JsonRequired]
        public string Description { get; set; }
    }
}