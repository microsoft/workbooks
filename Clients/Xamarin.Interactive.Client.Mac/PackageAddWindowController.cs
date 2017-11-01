//
// PackageAddWindowController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Threading;

using Foundation;
using AppKit;

using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class PackageAddWindowController : NSWindowController
    {
        readonly PackageViewModel package;
        readonly CancellationTokenSource cancellationTokenSource;

        public PackageAddWindowController (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public PackageAddWindowController (NSCoder coder) : base (coder)
        {
        }

        public PackageAddWindowController (PackageViewModel package, CancellationTokenSource cancellationTokenSource)
            : base ("PackageAddWindow")
        {
            this.package = package;
            this.cancellationTokenSource = cancellationTokenSource;
        }

        public override void AwakeFromNib ()
        {
            progressIndicator.StartAnimation (this);

            statusTextField.StringValue = $"Adding {package.DisplayName} to workbook…";

            cancelButton.Activated += (sender, e) => {
                cancelButton.Enabled = false;
                try {
                    cancellationTokenSource.Cancel ();
                } catch (ObjectDisposedException) { }
                Close ();
            };
        }

        public new PackageAddWindow Window {
            get { return (PackageAddWindow)base.Window; }
        }
    }
}