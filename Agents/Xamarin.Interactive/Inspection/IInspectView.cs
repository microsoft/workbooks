//
// IInspectView.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Inspection
{
	interface IInspectView
	{
		double X { get; set; }
		double Y { get; set; }
		double Width { get; set; }
		double Height { get; set; }
		void AddSubview (IInspectView subView);
	}
}
