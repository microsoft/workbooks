//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

namespace Xamarin.Interactive.Serialization
{
    public interface ISerializableObject
    {
        void Serialize (ObjectSerializer serializer);
    }

    static class ISerializableObjectExtensions
    {
        public static string SerializeToString (
            this ISerializableObject o,
            bool enableTypes = true,
            bool enableReferences = true)
        {
            var writer = new StringWriter ();
            new ObjectSerializer (writer, enableTypes, enableReferences).Object (o);
            return writer.ToString ();
        }
    }
}