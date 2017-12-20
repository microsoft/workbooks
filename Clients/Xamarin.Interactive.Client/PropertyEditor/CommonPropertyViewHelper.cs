//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.PropertyEditor;
using Xamarin.PropertyEditing.Drawing;

using Xamarin.Interactive.Representations;
using System;

namespace Xamarin.Interactive.Client.PropertyEditor
{
    class CommonPropertyViewHelper : IPropertyViewHelper
    {
        public virtual bool IsConvertable (Type type)
        {
            if (type == typeof (CommonSize)
                || type == typeof (CommonPoint)
                || type == typeof (CommonRectangle)
                || type == typeof (CommonColor)
                || type == typeof (CommonThickness))
                return true;

            return false;
        }

        public virtual object ToLocalValue (object local)
        {
            switch (local) {
            case Size size:
                return new CommonSize (size.Width, size.Height);
            case Color color:
                unchecked {
                    return new CommonColor ((byte)(color.Red * 255), (byte)(color.Blue * 255), (byte)(color.Green * 255), (byte)(color.Alpha / 255));
                }
            case Rectangle rectangle:
                return new CommonRectangle (rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            case Point point:
                return new CommonPoint (point.X, point.Y);
            case Thickness thickness:
                return new CommonThickness (left: thickness.Left, top: thickness.Top, right: thickness.Right, bottom: thickness.Bottom);
            default:
                return local;
            }
        }

        public object ToRemoteValue (object local)
        {
            switch (local) {
            case CommonColor color:
                return new Color (color.R / 255.0, color.G / 255.0, color.B / 255.0, color.A / 255.0);
            case CommonSize size:
                return new Size (size.Width, size.Height);
            case CommonRectangle rectangle:
                return new Rectangle (rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            case CommonPoint point:
                return new Point (point.X, point.Y);
            case CommonThickness thickness:
                return new Thickness (left: thickness.Left, top: thickness.Top, right: thickness.Right, bottom: thickness.Bottom);
            default:
                return local;
            }
        }
    }
}
