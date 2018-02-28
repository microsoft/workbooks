//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Serialization.Converters
{
    sealed class ISerializableObjectConverter : JsonConverter
    {
        public override bool CanConvert (Type objectType)
            => objectType == typeof (JsonPayload) || typeof (ISerializableObject).IsAssignableFrom (objectType);

        public override void WriteJson (
            Newtonsoft.Json.JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            switch (value) {
            case JsonPayload json:
                writer.WriteRawValue (json);
                break;
            case ISerializableObject _:
                writer.WriteRawValue (Representations.RepresentationManager.ToJson (value));
                break;
            case null:
                writer.WriteNull ();
                break;
            }
        }

        public override object ReadJson (
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
            => throw new NotImplementedException ();
    }
}