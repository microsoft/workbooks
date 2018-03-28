// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    [MonacoSerializable ("monaco.languages.Hover")]
    public struct Hover
    {
        public Range Range { get; }
        public IReadOnlyList<string> Contents { get; }

        [JsonConstructor]
        public Hover (
            Range range,
            IReadOnlyList<string> contents)
        {
            Range = range;
            Contents = contents;
        }
    }
}