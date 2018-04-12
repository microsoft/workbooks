// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Serialization
{
    static class JsonSerializerExtensions
    {
        const int BufferSize = 1024; // StreamReader's internal default size

        public async static Task DeserializeMultiple (
            this JsonSerializer serializer,
            Stream stream,
            Action<object> valueHandler,
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException (nameof (stream));

            if (valueHandler == null)
                throw new ArgumentNullException (nameof (valueHandler));

            using (var streamReader = new StreamReader (stream, Utf8.Encoding, false, BufferSize, true))
            using (var jsonReader = new JsonTextReader (streamReader) {
                CloseInput = false,
                SupportMultipleContent = true
            }) {
                while (await jsonReader.ReadAsync (cancellationToken))
                    valueHandler (serializer.Deserialize (jsonReader));
            }
        }

        public static object Deserialize (
            this JsonSerializer serializer,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException (nameof (stream));

            using (var streamReader = new StreamReader (stream, Utf8.Encoding, false, BufferSize, true))
            using (var jsonReader = new JsonTextReader (streamReader) { CloseInput = false })
                return serializer.Deserialize (jsonReader);
        }

        public static void Serialize (this JsonSerializer serializer, Stream stream, object value)
        {
            using (var streamWriter = new StreamWriter (stream, Utf8.Encoding, BufferSize, true))
            using (var jsonWriter = new JsonTextWriter (streamWriter)) {
                serializer.Serialize (jsonWriter, value);
                jsonWriter.Flush ();
            }
        }
    }
}