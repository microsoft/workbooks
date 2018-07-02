//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive
{
    struct Optional<T>
    {
        public bool HasValue { get; }
        public T Value { get; }

        public Optional (T value)
        {
            HasValue = true;
            Value = value;
        }

        public override string ToString ()
            => HasValue
                ? Value?.ToString () ?? "(null)"
                : "(unspecified)";

        public T GetValueOrDefault (T @default = default (T))
            => HasValue ? Value : @default;

        public static implicit operator Optional<T> (T value)
            => new Optional<T> (value);
    }
}