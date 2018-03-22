//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.XamPub.Models
{
    sealed class GitSource
    {
        [JsonProperty ("repository")]
        public string Repository { get; set; }

        [JsonProperty ("branch")]
        public string Branch { get; set; }

        [JsonProperty ("revision")]
        public string Revision { get; set; }

        [JsonProperty ("tags")]
        public List<GitTag> Tags { get; set; }
    }
}