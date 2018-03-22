//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.XamPub.Converters
{
    sealed class FlagsEnumConverter : JsonConverter
    {
        public override bool CanConvert (Type objectType)
            => objectType.IsEnum;

        public override object ReadJson (
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var isNullable =
                objectType.IsGenericType &&
                objectType.GetGenericTypeDefinition () == typeof (Nullable<>);

            if (isNullable)
                objectType = objectType.GenericTypeArguments [0];

            var value = string.Join (",", serializer.Deserialize<List<string>> (reader));

            if (string.IsNullOrEmpty (value)) {
                if (isNullable)
                    return null;

                return Activator.CreateInstance (objectType);
            }

            return Enum.Parse (objectType, value);
        }

        public override void WriteJson (
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            if (value == null) {
                writer.WriteNull ();
                return;
            }

            writer.WriteStartArray ();

            foreach (var enumValue in value.ToString ().Split (
                new [] { ',' },
                StringSplitOptions.RemoveEmptyEntries))
                writer.WriteValue (enumValue.Trim ());

            writer.WriteEndArray ();
        }
    }
}