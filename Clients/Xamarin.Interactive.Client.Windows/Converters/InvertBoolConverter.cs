// InvertBoolConverter.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.

using System;
using System.Globalization;
using System.Windows.Data;

namespace Xamarin.Interactive.Client.Windows.Converters
{
	class InvertBoolConverter : IValueConverter
	{
		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool)
				return !(bool) value;

			// If non-null non-bool values are normally truthy, then we convert those to falsey
			return value == null;
		}

		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Convert (value, targetType, parameter, culture);
		}
	}
}
