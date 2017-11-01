//
// QueryFrequencyConverter.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Globalization;
using System.Windows.Data;

using Xamarin.Interactive.Client.Updater;

namespace Xamarin.Interactive.Client.Windows.Converters
{
	sealed class QueryFrequencyConverter :IValueConverter
	{
		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			var queryFrequency = default (QueryFrequency);
			if (value is QueryFrequency)
				queryFrequency = (QueryFrequency)value;

			return queryFrequency.ToLocalizedName ();
		}

		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
	}
}