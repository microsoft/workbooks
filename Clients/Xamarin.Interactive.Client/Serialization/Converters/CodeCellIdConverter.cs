//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Serialization.Converters
{
    sealed class CodeCellIdConverter : JsonConverter
    {
        public override bool CanConvert (Type objectType)
            => objectType == typeof (CodeCellId);

        public override object ReadJson (
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
            => (CodeCellId)(string)reader.Value;

        public override void WriteJson (
            Newtonsoft.Json.JsonWriter writer,
            object value,
            JsonSerializer serializer)
            => writer.WriteValue ((string)(CodeCellId)value);
    }
}