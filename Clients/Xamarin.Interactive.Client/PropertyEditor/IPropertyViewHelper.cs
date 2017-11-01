// IPropertyViewHelper.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.PropertyEditor
{
	interface IPropertyViewHelper
	{
		object ToLocalValue (object prop);
		object ToRemoteValue (object localValue);
	}
}