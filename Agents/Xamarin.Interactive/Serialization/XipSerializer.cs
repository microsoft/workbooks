//
// XipSerializer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Serialization
{
	sealed class XipSerializer
	{
		[Serializable]
		struct XipNullGraph
		{
		}

		// DBNull is not serializable in .NET Core 2.0 (https://github.com/dotnet/corefx/issues/19119).
		// In case it comes up indirectly (which we've seen happen with reflection types),
		// provide a serialization surrogate for this singleton value.
		class DBNullSurrogate : ISerializationSurrogate
		{
			public void GetObjectData (object obj, SerializationInfo info, StreamingContext context)
			{
				// nothing to do
			}

			public object SetObjectData (
				object obj,
				SerializationInfo info,
				StreamingContext context,
				ISurrogateSelector selector)
				=> DBNull.Value;
		}

		readonly Stream stream;
		readonly BinaryFormatter formatter;

		public XipSerializer (Stream stream, XipSerializerSettings settings = null)
		{
			if (stream == null)
				throw new ArgumentNullException (nameof(stream));

			this.stream = stream;

			var context = new StreamingContext (StreamingContextStates.CrossProcess, new Context ());

			var xipBinder = settings?.Binder as XipSerializationBinder;
			if (xipBinder != null)
				xipBinder.Context = context;

			// Provide surrogates as necessary for compatibility with .NET Core 2.0.
			var surrogateSelector = new SurrogateSelector ();
			surrogateSelector.AddSurrogate (typeof (DBNull), context, new DBNullSurrogate ());

			formatter = new BinaryFormatter {
				Context = context,
				AssemblyFormat = FormatterAssemblyStyle.Simple,
				FilterLevel = TypeFilterLevel.Low,
				Binder = settings?.Binder,
				SurrogateSelector = surrogateSelector,
			};
		}

		public object Deserialize ()
		{
			lock (stream) {
				var value = formatter.Deserialize (stream);
				return value is XipNullGraph ? null : value;
			}
		}

		public void Serialize (object value)
		{
			lock (stream)
				formatter.Serialize (stream, value ?? new XipNullGraph ());
		}

		public sealed class Context
		{
			public RepresentedType XipObjectRepresentedType { get; internal set; }
		}
	}
}