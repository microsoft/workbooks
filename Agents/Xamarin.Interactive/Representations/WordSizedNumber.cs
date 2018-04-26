//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    sealed class WordSizedNumber
    {
        public string RepresentedType { get; }
        public int Size { get; } = IntPtr.Size;
        public WordSizedNumberFlags Flags { get; }
        public object Value { get; }

        [JsonConstructor]
        WordSizedNumber (
            string representedType,
            int size,
            WordSizedNumberFlags flags,
            object value)
        {
            RepresentedType = representedType;
            Size = size;
            Flags = flags;
            Value = value;
        }

        public WordSizedNumber (object value, WordSizedNumberFlags flags, ulong storage)
        {
            RepresentedType = value.GetType ().ToSerializableName ();
            Flags = flags;

            if (flags.HasFlag (WordSizedNumberFlags.Real)) {
                Value = BitConverter.Int64BitsToDouble ((long)storage);
                if (Size == 4)
                    Value = (float)(double)Value;
            } else if (flags.HasFlag (WordSizedNumberFlags.Signed)) {
                if (Size == 4)
                    Value = (int)(long)storage;
                else
                    Value = (long)storage;
            } else {
                if (Size == 4)
                    Value = (uint)storage;
                else
                    Value = storage;
            }
        }
    }
}