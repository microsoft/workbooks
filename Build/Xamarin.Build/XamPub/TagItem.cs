//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.XamPub
{
    [JsonObject]
    public sealed class TagItem
    {
        [JsonProperty ("tags")]
        public string [] Tags { get; set; }

        [JsonProperty ("message")]
        public string Message { get; set; }

        [JsonProperty ("hash")]
        public string CommitHash { get; set; }

        [JsonProperty ("githubRepo")]
        public string GitHubRepo { get; set; }
    }
}