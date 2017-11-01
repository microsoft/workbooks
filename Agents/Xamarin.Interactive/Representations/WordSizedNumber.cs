//
// WordSizedNumber.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
	[Serializable]
	sealed class WordSizedNumber : IRepresentationObject
	{
		public string RepresentedType { get; }
		public int Size { get; } = IntPtr.Size;
		public WordSizedNumberFlags Flags { get; }
		public object Value { get; }

		public WordSizedNumber (object value, WordSizedNumberFlags flags, ulong storage)
		{
			RepresentedType = value.GetType ().ToSerializableName ();
			Flags = flags;

			if (flags.HasFlag (WordSizedNumberFlags.Real)) {
				Value = BitConverter.Int64BitsToDouble ((long)storage);
				if (Size == 4)
					Value = (float)(double)Value;
			} else if (flags.HasFlag (WordSizedNumberFlags.Signed)) {
				if (Size == 4)
					Value = (int)(long)storage;
				else
					Value = (long)storage;
			} else {
				if (Size == 4)
					Value = (uint)storage;
				else
					Value = storage;
			}
		}

		void ISerializableObject.Serialize (ObjectSerializer serializer)
		{
			throw new NotImplementedException ();
		}
	}
}