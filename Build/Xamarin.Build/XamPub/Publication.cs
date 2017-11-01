//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

using Newtonsoft.Json;

namespace Xamarin.XamPub
{
    public sealed class Publication
    {
        [JsonProperty ("info")]
        public PublicationInfo Info { get; set; }

        [JsonProperty ("release")]
        public PublicationItem [] Release { get; set; }

        public void Write (TextWriter writer)
            => new JsonSerializer {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            }.Serialize (writer, this);
    }
}