//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

namespace Xamarin.XamPub.Converters
{
    sealed class GuidConverter : JsonConverter
    {
        public override bool CanConvert (Type objectType)
            => objectType == typeof (Guid);

        public override object ReadJson (
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var guidStr = reader.Value as string;
            return string.IsNullOrEmpty (guidStr)
                ? Guid.Empty
                : Guid.Parse (guidStr);
        }

        public override void WriteJson (
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
            => writer.WriteValue (value.ToString ());
    }
}