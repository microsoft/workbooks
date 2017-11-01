//
// ViewController.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using UIKit;

using Xamarin.Interactive.iOS;

namespace Xamarin.Workbooks.iOS
{
    public partial class ViewController : UIViewController
    {
        public ViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            var agent = new iOSAgent ();
            agent.IdentificationFailure += (sender, e) => {
                var alert = UIAlertController.Create (
                    "Error connecting to workbook",
                    "The workbook could not be reached. Please verify that this device has a " +
                    "working network connection. Visit http://xmn.io/workbooks-help for additional " +
                    "troubleshooting steps.",
                    UIAlertControllerStyle.Alert
                );
                alert.AddAction (UIAlertAction.Create ("OK", UIAlertActionStyle.Default, null));

                PresentViewController (alert, true, null);
            };
            agent.Start ();

            // TODO: Can we launch client app when debugging like we can for Mac?
        }
    }
}