// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    struct ToStringRepresentation
    {
        [JsonObject]
        public struct Format
        {
            public string Name { get; }
            public string Value { get; }

            [JsonConstructor]
            public Format (string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

        public IReadOnlyList<Format> Formats { get; }

        [JsonConstructor]
        public ToStringRepresentation (IReadOnlyList<Format> formats)
            => Formats = formats;

        public ToStringRepresentation (object value)
            => Formats = GetFormats (value)
                .Select (format => new Format (
                    format,
                    string.Format (format, value)))
                .ToList ();

        static readonly string [] defaultFormats = { "{0}" };
        static readonly string [] int8Formats = { "0x{0:x2}", "{0}" };
        static readonly string [] int16Formats = { "{0}", "0x{0:x}", "0x{0:x4}", "{0:N0}" };
        static readonly string [] int32Formats = { "{0}", "0x{0:x}", "0x{0:x8}", "{0:N0}" };
        static readonly string [] int64Formats = { "{0}", "0x{0:x}", "0x{0:x16}", "{0:N0}" };
        static readonly string [] realFormats = { "{0}", "{0:N}", "{0:C}" };
        static readonly string [] pointerFormats = { "0x{0:x}", "{0}" };

        static string [] GetFormats (object value)
        {
            switch (value) {
            case sbyte _:
            case byte _:
                return int8Formats;
            case short _:
            case ushort _:
                return int16Formats;
            case int _:
            case uint _:
                return int32Formats;
            case long _:
            case ulong _:
                return int64Formats;
            case float _:
            case double _:
            case decimal _:
                return realFormats;
            case WordSizedNumber word:
                if (word.Flags.HasFlag (WordSizedNumberFlags.Pointer))
                    return pointerFormats;

                if (word.Flags.HasFlag (WordSizedNumberFlags.Real))
                    return realFormats;

                if (word.Size == 4)
                    return int32Formats;

                if (word.Size == 8)
                    return int64Formats;

                goto default;
            default:
                return defaultFormats;
            }
        }
    }
}