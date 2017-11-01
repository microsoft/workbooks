//
// MainActivity.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.
// Copyright 2017 Microsoft. All rights reserved.

using Android.App;
using Android.OS;

namespace Xamarin.Workbooks.Android
{
	[Activity (
		Label = "Xamarin Workbooks App",
		MainLauncher = true,
		Icon = "@mipmap/icon",
		Name = "xamarin.workbooks.android.MainActivity"
	)]
	public class MainActivity : Activity
	{
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			var workbookApp = (WorkbookApplication) Application;
			workbookApp.Agent.ActivityTracker = new ActivityTrackerStub (this);
			workbookApp.Agent.Start ();
		}
	}
}


