//
// ViewTransform.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.
using System;

namespace Xamarin.Interactive.Remote
{
	[Serializable]
	class ViewTransform
	{
		public double M11 = 1;
		public double M12;
		public double M13;
		public double M14;
		public double M21;
		public double M22 = 1;
		public double M23;
		public double M24;
		public double M31;
		public double M32;
		public double M33 = 1;
		public double M34;
		public double OffsetX;
		public double OffsetY;
		public double OffsetZ;
		public double M44 = 1;
	}
}
