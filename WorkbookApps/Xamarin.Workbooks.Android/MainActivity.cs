//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
