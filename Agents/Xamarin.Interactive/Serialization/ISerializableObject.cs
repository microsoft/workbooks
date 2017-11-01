//
// ISerializableObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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