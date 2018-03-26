// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    [MonacoSerializable ("monaco.languages.Hover")]
    public struct Hover
    {
        public Range Range { get; }
        public string [] Contents { get; }

        [JsonConstructor]
        public Hover (
            Range range,
            string [] contents)
        {
            Range = range;
            Contents = contents;
        }
    }
}