//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using Newtonsoft.Json;

namespace Xamarin.XamPub.Models
{
    sealed class XamarinUpdaterProduct
    {
        [JsonProperty ("productGuid")]
        [JsonRequired]
        [JsonConverter (typeof (Converters.GuidConverter))]
        public Guid ProductGuid { get; set; }

        [JsonProperty ("releaseId")]
        [JsonRequired]
        public string ReleaseId { get; set; }

        [JsonProperty ("version")]
        [JsonRequired]
        public string Version { get; set; }

        [JsonProperty ("channels")]
        [JsonRequired]
        [JsonConverter (typeof (Converters.FlagsEnumConverter))]
        public XamarinUpdaterChannels Channels { get; set; }

        [JsonProperty ("blurb")]
        [JsonRequired]
        public string Blurb { get; set; }

        [JsonProperty ("versionAlias")]
        public string VersionAlias { get; set; }

        [JsonProperty ("isMajorVersion")]
        public bool IsMajorVersion { get; set; }

        [JsonProperty ("addinsVersion")]
        public string AddinsVersion { get; set; }

        [JsonProperty ("compatibleVersion")]
        public string CompatibleVersion { get; set; }

        [JsonProperty ("requiresRestart")]
        public bool RequiresRestart { get; set; }

        [JsonProperty ("requiresInteractiveInstall")]
        public bool RequiresInteractiveInstall { get; set; }

        [JsonProperty ("requireEnv")]
        public string RequireEnv { get; set; }

        [JsonProperty ("showEula")]
        public bool ShowEula { get; set; }

        public void PopulateFromUpdateinfoFile (string updateinfoFile)
        {
            var contents = File.ReadAllText (updateinfoFile);

            var parts = contents.Split (
                new [] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts != null && parts.Length == 2) {
                ProductGuid = Guid.Parse (parts [0]);
                ReleaseId = parts [1];
            }
        }
    }
}
