//
// TypeScriptConfig.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

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