//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace Xamarin.Interactive.Client.Windows.Converters
{
    class IsGreaterThanOrEqualToVisibilityConverter : BoolToVisibilityConverter
    {
        public override object Convert (object value, Type targetType, object parameter, CultureInfo culture)
        {
            int rhs;
            switch (parameter) {
            case string s:
                rhs = int.Parse (s, culture);
                break;
            case int i:
                rhs = i;
                break;
            default:
                throw new NotImplementedException ();
            }

            return base.Convert ((int)value >= rhs, targetType, null, culture);
        }

        public override object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException ();
    }
}