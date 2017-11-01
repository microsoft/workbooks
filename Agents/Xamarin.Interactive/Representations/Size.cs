//
// Size.cs
//
// Author:
//   Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright 2014-2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
	[Serializable]
	public sealed class Size : IRepresentationObject
	{
		public Size (double width, double height)
		{
			Width = width;
			Height = height;
		}

		public double Width { get; }
		public double Height { get; }

		void ISerializableObject.Serialize (ObjectSerializer serializer)
		{
			throw new NotImplementedException ();
		}
	}
}