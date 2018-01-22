//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.XamPub.Models
{
    sealed class ReleaseFile : FileBase
    {
        Guid id;

        [JsonProperty ("id")]
        [JsonConverter (typeof (Converters.GuidConverter))]
        public Guid Id {
            get => id == Guid.Empty ? (id = Guid.NewGuid ()) : id;
            set => id = value;
        }

        [JsonProperty ("productName")]
        [JsonRequired]
        public string ProductName { get; set; }

        [JsonProperty ("publishUri")]
        [JsonRequired]
        public string PublishUri { get; set; }

        [JsonProperty ("evergreenUri")]
        public string EvergreenUri { get; set; }

        [JsonProperty ("downloadUri")]
        public string DownloadUri { get; set; }

        [JsonProperty ("uploadEnvironments")]
        [JsonConverter (typeof (Converters.FlagsEnumConverter))]
        public UploadEnvironments UploadEnvironments { get; set; }

        [JsonProperty ("updaterProduct")]
        public XamarinUpdaterProduct UpdaterProduct { get; set; }

        [JsonProperty ("symbolFiles")]
        public List<SymbolFile> SymbolFiles { get; set; }

        [JsonProperty ("git")]
        public GitSource Git { get; set; }
    }
}