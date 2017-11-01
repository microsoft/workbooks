//
// WorkbookFormsApplication.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using Android.App;
using Android.Runtime;

using Xamarin.Interactive.Android;

namespace Xamarin.Workbooks.Android
{
    [Application]
    public class WorkbookApplication : Application
    {
        internal AndroidAgent Agent { get; private set; }

        public WorkbookApplication (IntPtr javaReference, JniHandleOwnership transfer) : base (javaReference, transfer)
        {
        }

        public override void OnCreate ()
        {
            base.OnCreate ();

            Agent = new AndroidAgent (null, contentId: global::Android.Resource.Id.Content);
            Agent.IdentificationFailure += OnAgentIdentificationFailure;
        }

        private void OnAgentIdentificationFailure (object sender, EventArgs e)
        {
            new AlertDialog.Builder (this)
                .SetTitle ("Error connecting to workbook")
                .SetMessage (
                    "The workbook could not be reached. Please verify that this device has a " +
                    "working network connection. Visit http://xmn.io/workbooks-help for additional " +
                    "troubleshooting steps.")
                .Show ();
        }
    }
}