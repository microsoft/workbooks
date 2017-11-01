// InspectViewAsContainerConverter.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

using Xamarin.Interactive.TreeModel;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Windows.Converters
{
	class InspectViewAsContainerConverter : AsContainerConverter<InspectView> { }
	class TreeNodeAsContainerConverter : AsContainerConverter<TreeNode> { }


	class AsContainerConverter<T> : IValueConverter
	{
		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return EmptyArray<T>.Instance;

			return new[] { (T) value };
		}

		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
		{
			var container = value as IEnumerable<T>;

			if (container != null)
				return container.Single ();

			return null;
		}
	}
}
