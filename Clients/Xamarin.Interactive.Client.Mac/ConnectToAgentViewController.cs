//
// ConnectToAgentViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Client.Mac
{
    partial class ConnectToAgentViewController : NSViewController
    {
        ClientSessionUri clientSessionUri;

        ConnectToAgentViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            liveInspectionRadioButton.State = NSCellStateValue.On;

            connectButton.KeyEquivalent = "\r";

            locationTextField.Changed += (sender, e) => {
                if (ClientSessionUri.TryParse (locationTextField?.StringValue, out clientSessionUri))
                    clientSessionUriTextField.StringValue = clientSessionUri.ToString ();
                else
                    clientSessionUriTextField.StringValue = "Invalid Location";

                ValidateUserInterface ();
            };

            ValidateUserInterface ();

            locationTextField.BecomeFirstResponder ();
        }

        void ValidateUserInterface ()
        {
            connectButton.Title = "Connect";
            if (clientSessionUri != null) {
                switch (clientSessionUri.SessionKind) {
                case ClientSessionKind.LiveInspection:
                    liveInspectionRadioButton.State = NSCellStateValue.On;
                    break;
                default:
                    workbookRadioButton.State = NSCellStateValue.On;
                    break;
                }
            }
        }

        [Action ("selectClientBehavior:")]
        void SelectClientBehavior (NSObject sender)
        {
        }

        [Action ("connect:")]
        void Connect (NSObject sender)
        {
            if (clientSessionUri != null)
                Open (clientSessionUri);
        }

        void Open (ClientSessionUri uri)
        {
            var sessionKind = liveInspectionRadioButton.State == NSCellStateValue.On
                ? ClientSessionKind.LiveInspection
                : ClientSessionKind.Workbook;

            if (NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (uri.WithSessionKind (sessionKind))))
                View.Window.Close ();
        }
    }
}