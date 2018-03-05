//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Serialization
{
    public sealed class ObjectSerializer
    {
        readonly TextWriter writer;
        readonly bool enableTypes;
        readonly Dictionary<object, int> references;

        int lastReferenceId;
        bool atObjectStart;
        int depth;

        internal ObjectSerializer (
            TextWriter writer,
            bool enableTypes = true,
            bool enableReferences = true)
        {
            if (writer == null)
                throw new ArgumentNullException (nameof (writer));

            this.writer = writer;

            this.enableTypes = enableTypes;

            if (enableReferences)
                references = new Dictionary<object, int> ();
        }

        public void Object (ISerializableObject value)
        {
            if (value == null)
                throw new ArgumentNullException (nameof (value));

            if (depth > 0)
                throw new InvalidOperationException (
                    $"{nameof (Object)} can only be called for the root value");

            ObjectUnchecked (value);
        }

        void ObjectUnchecked<TSerializable> (TSerializable value)
            where TSerializable : ISerializableObject
        {
            depth++;
            atObjectStart = true;

            try {
                var referenceId = -1;

                if (references != null && default (TSerializable) == null) {
                    if (references.TryGetValue (value, out referenceId)) {
                        writer.Write ("{\"$ref\":\"");
                        writer.WriteJson (referenceId);
                        writer.Write ("\"}");
                        return;
                    }

                    referenceId = lastReferenceId++;
                    references.Add (value, referenceId);
                }

                writer.Write ('{');

                if (enableTypes) {
                    atObjectStart = false;
                    writer.Write ("\"$type\":\"");
                    writer.Write (value.GetType ().ToSerializableName ());
                    writer.Write ('"');
                }

                if (referenceId >= 0) {
                    if (!atObjectStart)
                        writer.Write (',');
                    atObjectStart = false;
                    writer.Write ("\"$id\":\"");
                    writer.WriteJson (referenceId);
                    writer.Write ('"');
                }

                value.Serialize (this);

                writer.Write ('}');
            } finally {
                depth--;
            }
        }

        void StartProperty (string name)
        {
            if (name == null)
                throw new ArgumentNullException (nameof (name));

            if (!atObjectStart)
                writer.Write (',');

            atObjectStart = false;

            writer.WriteJson (name);
            writer.Write (':');
        }

        public void Property<TSerializable> (string name, TSerializable value)
            where TSerializable : ISerializableObject
        {
            if ((object)value == null || value.Equals (default (TSerializable)))
                return;

            StartProperty (name);
            ObjectUnchecked (value);
        }

        public void Property (string name, bool value)
        {
            StartProperty (name);
            writer.WriteJson (value);
        }

        public void Property (string name, int value)
        {
            StartProperty (name);
            writer.WriteJson (value);
        }

        public void Property (string name, float value)
        {
            StartProperty (name);
            writer.WriteJson (value);
        }

        public void Property (string name, double value)
        {
            StartProperty (name);
            writer.WriteJson (value);
        }

        public void Property (string name, string value)
            => Property (name, value, PropertyOptions.Default);

        public void Property (string name, string value, PropertyOptions options)
        {
            if (value == null && !options.HasFlag (PropertyOptions.SerializeIfNull))
                return;

            if (value == string.Empty && !options.HasFlag (PropertyOptions.SerializeIfEmpty))
                return;

            StartProperty (name);
            writer.WriteJson (value);
        }

        public void Property (string name, byte [] value)
            => Property (name, value, PropertyOptions.Default);

        public void Property (string name, byte [] value, PropertyOptions options)
        {
            if (value == null && !options.HasFlag (PropertyOptions.SerializeIfNull))
                return;

            if (value.Length == 0 && !options.HasFlag (PropertyOptions.SerializeIfEmpty))
                return;

            StartProperty (name);
            writer.WriteJson (value);
        }

        void Property<T> (string name, IEnumerable<T> value, Action<T> childWriter)
        {
            if (value == null)
                return;

            StartProperty (name);

            writer.Write ('[');

            var atArrayStart = true;

            foreach (var child in value) {
                if (!atArrayStart)
                    writer.Write (',');

                atArrayStart = false;

                childWriter (child);
            }

            writer.Write (']');
        }

        public void Property<TSerializable> (string name, IEnumerable<TSerializable> value)
            where TSerializable : ISerializableObject
            => Property (name, value, ObjectUnchecked);

        public void Property (string name, IEnumerable<bool> value)
            => Property (name, value, writer.WriteJson);

        public void Property (string name, IEnumerable<int> value)
            => Property (name, value, writer.WriteJson);

        public void Property (string name, IEnumerable<float> value)
            => Property (name, value, writer.WriteJson);

        public void Property (string name, IEnumerable<double> value)
            => Property (name, value, writer.WriteJson);

        public void Property (string name, IEnumerable<string> value)
            => Property (name, value, writer.WriteJson);

        public void Property (string name, IEnumerable<byte []> value)
            => Property (name, value, writer.WriteJson);

        public void Property (string name, object value)
        {
            switch (value) {
            case InteractiveObjectBase obj:
            case RepresentedObject rep:
                Property (name, new Representations.JsonRepresentation (value));
                break;
            case ISerializableObject json:
                Property (name, json);
                break;
            case int _:
            case short _:
            case SByte _:
            case uint _:
            case ushort _:
            case byte _:
            case double _:
            case float _:
            case Enum _:
                Property (name, (double)value);
                break;
            case long _:
            default:
                Property (name, value?.ToString ());
                break;

            }
        }
    }
}