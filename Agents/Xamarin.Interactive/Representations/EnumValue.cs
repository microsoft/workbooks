//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

using Newtonsoft.Json;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    sealed class EnumValue
    {
        public RepresentedType RepresentedType { get; }
        public RepresentedType UnderlyingType { get; }
        public object Value { get; }
        public IReadOnlyList<object> Values { get; }
        public IReadOnlyList<string> Names { get; }
        public bool IsFlags { get; }

        [JsonConstructor]
        EnumValue (
            RepresentedType representedType,
            RepresentedType underlyingType,
            object value,
            IReadOnlyList<object> values,
            IReadOnlyList<string> names,
            bool isFlags)
        {
            RepresentedType = representedType;
            UnderlyingType = underlyingType;
            Value = value;
            Values = values;
            Names = names;
            IsFlags = isFlags;
        }

        public EnumValue (Enum value)
        {
            var type = value.GetType ();
            var underlyingType = Enum.GetUnderlyingType (type);

            RepresentedType = RepresentedType.Lookup (type);
            UnderlyingType = RepresentedType.Lookup (underlyingType);

            Value = Convert.ChangeType (value, underlyingType, CultureInfo.InvariantCulture);
            Names = Enum.GetNames (type);
            IsFlags = type.IsDefined (typeof (FlagsAttribute), false);

            var values = Enum.GetValues (type);
            var convertedValues = new object [values.Length];
            for (int i = 0; i < values.Length; i++)
                convertedValues [i] = Convert.ChangeType (
                    values.GetValue (i), underlyingType, CultureInfo.InvariantCulture);
            Values = convertedValues;
        }
    }
}