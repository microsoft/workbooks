//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
