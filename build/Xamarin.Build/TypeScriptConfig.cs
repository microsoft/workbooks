//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

using Newtonsoft.Json;

namespace Xamarin.Interactive.MSBuild
{
    public sealed class TypeScriptConfig
    {
        [JsonProperty ("files")]
        public string [] Files { get; set; }

        public static TypeScriptConfig ReadFromFile (string path)
        {
            using (var reader = new StreamReader (path))
                return new JsonSerializer ().Deserialize<TypeScriptConfig> (
                    new JsonTextReader (reader));
        }
    }
}