//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.XamPub
{
    public sealed class PublicationInfo
    {
        [JsonProperty ("name")]
        public string Name { get; set; }
    }
}