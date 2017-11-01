// MultipleToVisibilityConverter.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Xamarin.Interactive.Client.Windows.Converters
{
	class MultipleToVisibilityConverter : MultipleToTypeConverter<Visibility> { }

	class MultipleToTypeConverter<T> : PredicateToTypeConverter<T>
	{
		public int Minimum { get; set; } = 1;
	
		public MultipleToTypeConverter ()
		{
			predicate = IsMultiple;
		}

		bool IsMultiple (object value)
		{
			var container = value as IList;
			return (container?.Count ?? 0) > Minimum;
		}
	}

	class PredicateToTypeConverter<T> : BoolToTypeConverter<T>, IValueConverter
	{
		protected Predicate<object> predicate;

		object IValueConverter.Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			return base.Convert (predicate(value), targetType, parameter, culture);
		}

		object IValueConverter.ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
	}
}
