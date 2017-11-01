//
// Rectangle.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
	[Serializable]
	public sealed class Rectangle : IRepresentationObject
	{
		public Rectangle (double x, double y, double width, double height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public double X { get; }
		public double Y { get; }
		public double Width { get; }
		public double Height { get; }

		void ISerializableObject.Serialize (ObjectSerializer serializer)
		{
			throw new NotImplementedException ();
		}
	}
}