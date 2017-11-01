//
// EnumValue.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Globalization;

using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    sealed class EnumValue : IRepresentationObject
    {
        readonly RepresentedType representedType;
        readonly RepresentedType underlyingType;
        readonly object value;
        readonly object [] values;
        readonly string [] names;
        readonly bool isFlags;

        public RepresentedType RepresentedType {
            get { return representedType; }
        }

        public RepresentedType UnderlyingType {
            get { return underlyingType; }
        }

        public object Value {
            get { return value; }
        }

        public object [] Values {
            get { return values; }
        }

        public string [] Names {
            get { return names; }
        }

        public bool IsFlags {
            get { return isFlags; }
        }

        public EnumValue (Enum value)
        {
            var type = value.GetType ();
            var underlyingType = Enum.GetUnderlyingType (type);

            representedType = RepresentedType.Lookup (type);
            this.underlyingType = RepresentedType.Lookup (underlyingType);

            this.value = Convert.ChangeType (value, underlyingType, CultureInfo.InvariantCulture);
            names = Enum.GetNames (type);
            isFlags = type.IsDefined (typeof (FlagsAttribute), false);

            var values = Enum.GetValues (type);
            this.values = new object [values.Length];
            for (int i = 0; i < values.Length; i++)
                this.values [i] = Convert.ChangeType (
                    values.GetValue (i), underlyingType, CultureInfo.InvariantCulture);
        }

        void ISerializableObject.Serialize (ObjectSerializer serializer)
        {
            throw new NotImplementedException ();
        }
    }
}