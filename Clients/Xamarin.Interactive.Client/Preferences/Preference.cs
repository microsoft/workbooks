//
// Preference.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Globalization;

namespace Xamarin.Interactive.Preferences
{
	sealed class Preference<T>
	{
		public string Key { get; }
		public T DefaultValue { get; }

		public Preference (string key, T defaultValue = default (T))
		{
			if (key == null)
				throw new ArgumentNullException (nameof (key));

			Key = key;
			DefaultValue = defaultValue;
		}

		public T GetValue ()
		{
			MainThread.Ensure ();

			var type = typeof (T);

			if (type.IsEnum)
				return (T)Enum.Parse (
					type,
					PreferenceStore.Default.GetString (Key, DefaultValue.ToString ()),
					true);

			var typeCode = Type.GetTypeCode (type);

			switch (typeCode) {
			// Builtin IPreferenceStore types
			case TypeCode.Boolean:
				return (T)(object)PreferenceStore.Default.GetBoolean (Key, (bool)(object)DefaultValue);
			case TypeCode.Int64:
				return (T)(object)PreferenceStore.Default.GetInt64 (Key, (long)(object)DefaultValue);
			case TypeCode.Double:
				return (T)(object)PreferenceStore.Default.GetDouble (Key, (double)(object)DefaultValue);
			case TypeCode.String:
				return (T)(object)PreferenceStore.Default.GetString (Key, (string)(object)DefaultValue);

			// Conversion types
			case TypeCode.Single:
				return (T)(object)unchecked((float)PreferenceStore.Default.GetDouble (
					Key,
					unchecked((float)(object)DefaultValue)));
			case TypeCode.SByte:
				return (T)(object)unchecked((sbyte)PreferenceStore.Default.GetInt64 (
					Key,
					unchecked((sbyte)(object)DefaultValue)));
			case TypeCode.Byte:
				return (T)(object)unchecked((byte)PreferenceStore.Default.GetInt64 (
					Key,
					unchecked((byte)(object)DefaultValue)));
			case TypeCode.Int16:
				return (T)(object)unchecked((short)PreferenceStore.Default.GetInt64 (
					Key,
					unchecked((short)(object)DefaultValue)));
			case TypeCode.UInt16:
				return (T)(object)unchecked((ushort)PreferenceStore.Default.GetInt64 (
					Key,
					unchecked((ushort)(object)DefaultValue)));
			case TypeCode.Int32:
				return (T)(object)unchecked((int)PreferenceStore.Default.GetInt64 (
					Key,
					unchecked((int)(object)DefaultValue)));
			case TypeCode.UInt32:
				return (T)(object)unchecked((uint)PreferenceStore.Default.GetInt64 (
					Key,
					unchecked((uint)(object)DefaultValue)));
			case TypeCode.UInt64:
				return (T)(object)unchecked((ulong)PreferenceStore.Default.GetInt64 (
					Key,
					unchecked((long)(ulong)(object)DefaultValue)));
			case TypeCode.DateTime:
				return (T)(object)DateTime.Parse (
					PreferenceStore.Default.GetString (
						Key,
						"0001-01-01T00:00:00.0000000"),
					CultureInfo.InvariantCulture,
					DateTimeStyles.RoundtripKind);
			}

			if (type == typeof (string []))
				return (T)(object)PreferenceStore.Default.GetStringArray (
					Key,
					(string [])(object)DefaultValue);

			throw new NotSupportedException ($"preference type not supported: {type}");
		}

		public void SetValue (T value)
		{
			MainThread.Ensure ();

			var type = typeof (T);

			if (type.IsEnum) {
				PreferenceStore.Default.Set (Key, value.ToString ());
				return;
			}

			var typeCode = Type.GetTypeCode (type);

			switch (typeCode) {
			// Builtin IPreferenceStore types
			case TypeCode.Boolean:
				PreferenceStore.Default.Set (Key, (bool)(object)value);
				return;
			case TypeCode.Int64:
				PreferenceStore.Default.Set (Key, (long)(object)value);
				return;
			case TypeCode.Double:
				PreferenceStore.Default.Set (Key, (double)(object)value);
				return;
			case TypeCode.String:
				PreferenceStore.Default.Set (Key, (string)(object)value ?? String.Empty);
				return;

			// Conversion types
			case TypeCode.Single:
				PreferenceStore.Default.Set (Key, unchecked((float)(object)value));
				return;
			case TypeCode.SByte:
				PreferenceStore.Default.Set (Key, unchecked((sbyte)(object)value));
				return;
			case TypeCode.Byte:
				PreferenceStore.Default.Set (Key, unchecked((byte)(object)value));
				return;
			case TypeCode.Int16:
				PreferenceStore.Default.Set (Key, unchecked((short)(object)value));
				return;
			case TypeCode.UInt16:
				PreferenceStore.Default.Set (Key, unchecked((ushort)(object)value));
				return;
			case TypeCode.Int32:
				PreferenceStore.Default.Set (Key, unchecked((int)(object)value));
				return;
			case TypeCode.UInt32:
				PreferenceStore.Default.Set (Key, unchecked((uint)(object)value));
				return;
			case TypeCode.UInt64:
				PreferenceStore.Default.Set (Key, unchecked((long)(ulong)(object)value));
				return;
			case TypeCode.DateTime:
				PreferenceStore.Default.Set (
					Key,
					((DateTime)(object)value).ToUniversalTime ().ToString ("O"));
				return;
			}

			if (type == typeof (string [])) {
				PreferenceStore.Default.Set (Key, (string [])(object)value);
				return;
			}

			throw new NotSupportedException ($"preference type not supported: {type}");
		}

		public void Reset ()
		{
			MainThread.Ensure ();
			PreferenceStore.Default.Remove (Key);
		}
	}
}