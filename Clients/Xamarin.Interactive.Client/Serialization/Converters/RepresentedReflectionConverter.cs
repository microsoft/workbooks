//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Serialization.Converters
{
    sealed class RepresentedReflectionConverter : JsonConverter
    {
        public override bool CanConvert (Type objectType)
            => objectType == typeof (RepresentedType);

        public override void WriteJson (
            Newtonsoft.Json.JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            switch (value) {
            case RepresentedType representedType:
                writer.WriteValue (representedType.Name);
                break;
            default:
                throw new NotImplementedException ();
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