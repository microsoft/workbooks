//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis
{
    public struct LanguageDescription
    {
        public string Name { get; }
        public string Version { get; }

        [JsonConstructor]
        public LanguageDescription (string name, string version = null)
        {
            Name = name;
            Version = version;
        }

        public override string ToString()
            => $"{Name},{Version}";

        public static implicit operator LanguageDescription (string languageId)
            => new LanguageDescription (languageId);
    }
}