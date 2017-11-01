//
// MacPropertyHelper.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using CoreGraphics;
using Xamarin.Interactive.PropertyEditor;
using XIR = Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Client.Mac
{
	sealed class MacPropertyHelper : IPropertyViewHelper
	{
		public object ToLocalValue (object prop)
		{
			switch (prop) {
				case XIR.Point point:
				return new CGPoint ((nfloat)point.X, (nfloat)point.Y);
				case XIR.Size size:
				return new CGSize ((nfloat)size.Width, (nfloat)size.Height);
				case XIR.Rectangle r:
				return new CGRect ((nfloat)r.X,(nfloat)r.Y, (nfloat)r.Width, (nfloat)r.Height);
				case XIR.Color c when c.ColorSpace == XIR.ColorSpace.Rgb:
				return new CGColor ((nfloat)c.Red, (nfloat)c.Green, (nfloat)c.Blue, (nfloat)c.Alpha);
			}
			return prop;
		}

		public object ToRemoteValue (object localValue) {
			switch (localValue) {
				case CGPoint point:
				return new XIR.Point (point.X, point.Y);
				case CGSize size:
				return new XIR.Size (size.Width, size.Height);
				case CGRect rect:
				return new XIR.Rectangle (rect.X, rect.Y, rect.Width, rect.Height);
			}
			return localValue;
		}
	}
}
