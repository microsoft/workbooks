//
// TagItem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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