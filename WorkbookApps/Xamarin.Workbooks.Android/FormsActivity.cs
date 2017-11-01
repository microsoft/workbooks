//
// MainActivity.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Xamarin.Workbooks.Android
{
    [Activity (
        Label = "Xamarin Workbooks App (Forms)",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public class FormsActivity : Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate (Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate (bundle);

            var app = (WorkbookApplication) Application;
            app.Agent.ActivityTracker = new ActivityTrackerStub (this);

            Forms.Forms.Init (this, bundle);
            LoadApplication (new WorkbookFormsApplication ());
        }
    }
}

