//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Representations;
using Xamarin.PropertyEditing;

namespace Xamarin.Interactive.PropertyEditor
{
    class InteractiveEnumPropertyInfo<T>
        : InteractivePropertyInfo, IHavePredefinedValues<T>
    {
        public bool IsValueCombinable { get; }

        public bool IsConstrainedToPredefined { get; }

        public IReadOnlyDictionary<string, T> PredefinedValues { get; }

        public InteractiveEnumPropertyInfo (InteractiveObjectEditor editor, int index)
            : base (editor, index)
        {
            var enumValue = GetEnumValue ();
            string [] names = enumValue.Names;
            Array values = enumValue.Values;

            var predefinedValues = new Dictionary<string, T> (names.Length);
            for (int i = 0; i < names.Length; i++) {
                predefinedValues.Add (names [i], (T)values.GetValue (i));
            }

            IsConstrainedToPredefined = true;
            PredefinedValues = predefinedValues;
            IsValueCombinable = enumValue.IsFlags;
        }

        EnumValue GetEnumValue () =>
            GetRepresentation (Value, Editor) as EnumValue;

        public override object ToRemoteValue<TValue> (object local)
        {
            switch (local) {
            case IReadOnlyList<T> values:
                if (!IsValueCombinable)
                    throw new ArgumentException (
                        "Can not set a combined value on a non-combinable type",
                            nameof (local));

                T realValue = values.Count > 0 ? values [0] : default (T);
                for (int i = 1; i < values.Count; i++) {
                    object v = values [i];
                    object result = realValue;

                    switch (result) {
                    case sbyte _:
                    case short _:
                    case int _:
                    case long _:
                        result = (long)result | (long)v;
                        break;
                    case byte _:
                    case ushort _:
                    case uint _:
                    case ulong _:
                        result = (ulong)result | (ulong)v;
                        break;
                    }
                    realValue = (T)result;
                }
                return realValue;
            default:
                return local;
            }
        }

        public override TValue ToLocalValue<TValue> ()
        {
            var enumValue = GetEnumValue ();
            switch (default (TValue)) {
            case IReadOnlyList<T> _:
                T realValue = (T)enumValue.Value;

                List<T> values = new List<T> ();
                foreach (T flag in PredefinedValues.Values) {
                    HasFlag (enumValue, flag);
                    values.Add (flag);
                }
                return (TValue)(object)values;
            default:
                return (TValue)enumValue.Value;
            }
        }

        public static bool HasFlag (EnumValue ev, T flag)
        {
            var value = ev.Value;

            switch (value) {
            case sbyte _:
            case short _:
            case int _:
            case long _:
                var convertedFlag = (long)Convert.ChangeType (flag, typeof (long), System.Globalization.CultureInfo.InvariantCulture);
                var convertedValue = (long)Convert.ChangeType (value, typeof (long), System.Globalization.CultureInfo.InvariantCulture);
                if ((convertedValue & convertedFlag) == convertedFlag)
                    return true;
                return false;
            case System.Enum _:
            case byte _:
            case ushort _:
            case uint _:
            case ulong _:
                var uConvertedFlag = (ulong)Convert.ChangeType (flag, typeof (ulong), System.Globalization.CultureInfo.InvariantCulture);
                var uConvertedValue = (ulong)Convert.ChangeType (value, typeof (ulong), System.Globalization.CultureInfo.InvariantCulture);
                if ((uConvertedValue & uConvertedFlag) == uConvertedFlag)
                    return true;
                return false;
            default:
                return false;
            }
        }
    }
}
