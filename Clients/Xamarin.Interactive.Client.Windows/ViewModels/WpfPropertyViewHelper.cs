//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Windows.Media;

using Xamarin.Interactive.PropertyEditor;

using XIR = Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Client.Windows.ViewModels
{
    class WpfPropertyViewHelper : IPropertyViewHelper
    {
        public object ToLocalValue (object prop)
        {
            var color = prop as XIR.Color;
            if (color != null) {
                return Color.FromArgb (
                    (byte)(color.Alpha * 255),
                    (byte)(color.Red * 255),
                    (byte)(color.Green * 255),
                    (byte)(color.Blue * 255));
            }

            return prop;
        }

        public object ToRemoteValue (object localValue)
        {
            var remoteVal = localValue;

            if (localValue is Color) {
                var c = (Color) localValue;

                remoteVal = new XIR.Color (
                    c.R / 255f,
                    c.G / 255f,
                    c.B / 255f,
                    c.A / 255f
                );
            }

            return remoteVal;
        }
    }
}