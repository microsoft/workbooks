//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    /// <summary>
    /// Provides a fallback "box" for (poorly) representing an CLR object in JSON.
    /// It provides a "$type" property that is the full CLR type name (non-assembly-qualified),
    /// and a "$toString" property that is either a string or an array of various ToString
    /// representations with various cultures and formats. The array variation of the
    /// "$toString" property is to represent numbers in various formats. Further,
    /// System.Boolean will have a "$value":bool property.
    /// </summary>
    struct JsonRepresentation : ISerializableObject
    {
        public object Value { get; }

        public JsonRepresentation (object value)
        {
            if (value == null)
                throw new ArgumentNullException (nameof (value));

            Value = value;
        }

        public void Serialize (ObjectSerializer serializer)
        {
            var formattableValue = Value;

            var word = Value as WordSizedNumber;

            if (Value is Image) {
                var image = Value as Image;
                serializer.Property ("data", image.Data);
                serializer.Property ("width", image.Width);
                serializer.Property ("height", image.Height);
                serializer.Property ("scale", image.Scale);
                switch (image.Format) {
                case ImageFormat.Jpeg:
                    serializer.Property ("format","image/jpeg");
                    break;
                case ImageFormat.Png:
                    serializer.Property ("format", "image/png");
                    break;
                case ImageFormat.Gif:
                    serializer.Property ("format", "image/gif");
                    break;
                case ImageFormat.Uri:
                    serializer.Property ("format", "uri");
                    break;
                case ImageFormat.Svg:
                    serializer.Property ("format", "image/svg+xml");
                    break;
                case ImageFormat.Unknown:
                    serializer.Property ("format", "image");
                    break;
                }
            }

            if (Value is RepresentedObject)
            {
                var representation = Value as RepresentedObject;
                serializer.Property("representedType", representation.RepresentedType?.Name);
                for (var i = 0; i < representation.Count; i++) {
                    serializer.Property (i.ToString (), new JsonRepresentation (representation [i]));
                }
            }

            if (Value is InteractiveObject) {
                var reflectedObject = Value as InteractiveObject;
                serializer.Property ("handle", reflectedObject.Handle.ToString ());
                serializer.Property ("representedObjectHandle", reflectedObject.RepresentedObjectHandle.ToString());
                serializer.Property ("isExpanded", reflectedObject.Members != null || !reflectedObject.HasMembers);
                if (reflectedObject.Members != null) {
                    for (var i = 0; i < reflectedObject.Members.Count(); i++)
                    {
                        var member = reflectedObject.Members[i];
                        var name = member.Name;
                        var value = reflectedObject.Values[i];
                        switch (value) {
                        case RepresentedObject _:
                        case InteractiveObject _:
                            serializer.Property (name, new JsonRepresentation (value));
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
                            serializer.Property (name, (double)value);
                            break;
                        case long _:
                        default:
                            serializer.Property (name, value?.ToString ());
                            break;
                        }
                    }
                }
            }

            if (word != null) {
                serializer.Property ("$type", word.RepresentedType);
                formattableValue = word.Value;
            } else {
                serializer.Property ("$type", Value.GetType ().ToSerializableName ());
            }

            if (Value is bool) {
                serializer.Property ("$value", (bool)Value);
                return;
            }

            var formats = GetFormats ();
            if (formats == null) {
                if (Value is string || Value.GetType ().ToString () != Value.ToString ())
                    serializer.Property (
                        "$toString",
                        Value.ToString (),
                        PropertyOptions.Default | PropertyOptions.SerializeIfNull);
                return;
            }

            serializer.Property (
                "$toString",
                new [] {
                    CultureInfo.CurrentCulture,
                    CultureInfo.InvariantCulture
                }.Select (c => new CulturedToStringFormats (c, formats, formattableValue)));
        }

        struct CultureInfoSerializable : ISerializableObject
        {
            readonly CultureInfo cultureInfo;

            public CultureInfoSerializable (CultureInfo cultureInfo)
            {
                this.cultureInfo = cultureInfo;
            }

            public void Serialize (ObjectSerializer serializer)
            {
                serializer.Property ("name", cultureInfo.Name);
                serializer.Property ("lcid", cultureInfo.LCID);
            }
        }

        struct CulturedToStringFormats : ISerializableObject
        {
            readonly CultureInfoSerializable culture;
            readonly IEnumerable<ToStringFormat> formats;

            public CulturedToStringFormats (CultureInfo cultureInfo, string [] formats, object value)
            {
                this.culture = new CultureInfoSerializable (cultureInfo);
                this.formats = formats
                    .Select (f => new ToStringFormat (
                        f.Replace ("{0", "{value"),
                        string.Format (cultureInfo, f, value)));
            }

            public void Serialize (ObjectSerializer serializer)
            {
                serializer.Property ("culture", culture);
                serializer.Property ("formats", formats);
            }
        }

        struct ToStringFormat : ISerializableObject
        {
            readonly string format;
            readonly string value;

            public ToStringFormat (string format, string value)
            {
                this.format = format;
                this.value = value;
            }

            public void Serialize (ObjectSerializer serializer)
                => serializer.Property (
                    format,
                    value,
                    PropertyOptions.Default | PropertyOptions.SerializeIfNull);
        }

        static readonly string [] int8Formats = { "0x{0:x2}", "{0}" };
        static readonly string [] int16Formats = { "{0}", "0x{0:x}", "0x{0:x4}", "{0:N0}" };
        static readonly string [] int32Formats = { "{0}", "0x{0:x}", "0x{0:x8}", "{0:N0}" };
        static readonly string [] int64Formats = { "{0}", "0x{0:x}", "0x{0:x16}", "{0:N0}" };
        static readonly string [] realFormats = { "{0}", "{0:N}", "{0:C}" };
        static readonly string [] pointerFormats = { "0x{0:x}", "{0}" };

        string [] GetFormats ()
        {
            switch (Type.GetTypeCode (Value.GetType ())) {
            case TypeCode.SByte:
            case TypeCode.Byte:
                return int8Formats;
            case TypeCode.Int16:
            case TypeCode.UInt16:
                return int16Formats;
            case TypeCode.Int32:
            case TypeCode.UInt32:
                return int32Formats;
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return int64Formats;
            case TypeCode.Double:
            case TypeCode.Single:
            case TypeCode.Decimal:
                return realFormats;
            default:
                var word = Value as WordSizedNumber;
                if (word == null)
                    return null;

                if (word.Flags.HasFlag (WordSizedNumberFlags.Pointer))
                    return pointerFormats;

                if (word.Flags.HasFlag (WordSizedNumberFlags.Real))
                    return realFormats;

                if (word.Size == 4)
                    return int32Formats;

                if (word.Size == 8)
                    return int64Formats;

                return null;
            }
        }
    }
}