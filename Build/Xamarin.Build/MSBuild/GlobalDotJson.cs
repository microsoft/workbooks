//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;

namespace Xamarin.MSBuild
{
    public sealed class GlobalDotJson : Task
    {
        [Required]
        public string FileName { get; set; }

        [Output]
        public string AppVersion { get; private set; }

        public override bool Execute ()
        {
            AppVersion = GlobalDotJsonData.ReadFromFile (FileName).App?.Version;
            return true;
        }

        public sealed class GlobalDotJsonData
        {
            public sealed class AppData
            {
                [JsonProperty ("version")]
                public string Version { get; set; }
            }

            [JsonProperty ("app")]
            public AppData App { get; set; }

            public static GlobalDotJsonData ReadFromFile (string path)
            {
                using (var reader = new StreamReader (path))
                    return new JsonSerializer ().Deserialize<GlobalDotJsonData> (
                        new JsonTextReader (reader));
            }
        }
    }
}