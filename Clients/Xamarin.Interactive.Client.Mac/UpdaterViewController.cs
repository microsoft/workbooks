//
// UpdaterViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.ComponentModel;

using AppKit;
using Foundation;

using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class UpdaterViewController : NSViewController
    {
        static readonly NSNumberFormatter percentFormatter = new NSNumberFormatter {
            NumberStyle = NSNumberFormatterStyle.Percent,
            MinimumFractionDigits = 0,
            MaximumFractionDigits = 0
        };

        static readonly NSByteCountFormatter byteFormatter = new NSByteCountFormatter {
            CountStyle = NSByteCountFormatterCountStyle.File,
            FormattingContext = NSFormattingContext.Standalone,
            ZeroPadsFractionDigits = true,
            AllowsNonnumericFormatting = false
        };

        UpdaterViewModel viewModel;

        UpdaterViewController (IntPtr handle) : base (handle)
        {
        }

        public bool CanCancel => viewModel.IsCancelButtonVisible;

        public void Cancel () => viewModel.CancelDownload ();

        public override void ViewDidLoad ()
        {
            progressBar.MinValue = 0;
            progressBar.MaxValue = 1;

            remindMeLaterButton.Activated += (sender, e) => {
                viewModel.RemindMeLater ();
                View.Window.Close ();
            };

            downloadButton.Activated += (sender, e) => viewModel.StartDownloadAsync ().Forget ();

            cancelButton.Activated += (sender, e) => Cancel ();

            webView.DecidePolicyForNavigation += (sender, e) => {
                if (e.NavigationType == WebKit.WebNavigationType.LinkClicked) {
                    NSWorkspace.SharedWorkspace.OpenUrl (e.OriginalUrl);
                    WebKit.WebView.DecideIgnore (e.DecisionToken);
                    return;
                }

                WebKit.WebView.DecideUse (e.DecisionToken);
            };

            base.ViewDidLoad ();
        }

        public void PresentUpdate (UpdateItem updateItem)
        {
            viewModel = new MacUpdaterViewModel (View.Window, updateItem);
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.DownloadItem.PropertyChanged += DownloadItem_PropertyChanged;

            ViewModel_PropertyChanged (null, null);
            DownloadItem_PropertyChanged (null, null);

            webView.MainFrame.LoadHtmlString (updateItem.ReleaseNotes, NSBundle.MainBundle.ResourceUrl);

            View.Window.MakeKeyAndOrderFront (this);
        }

        void ViewModel_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            progressBar.Hidden = !viewModel.IsProgressBarVisible;
            progressLabel.Hidden = !viewModel.IsProgressBarVisible;
            remindMeLaterButton.Hidden = !viewModel.IsRemindMeLaterButtonVisible;
            downloadButton.Hidden = !viewModel.IsDownloadButtonVisible;
            cancelButton.Hidden = !viewModel.IsCancelButtonVisible;

            remindMeLaterButton.StringValue = viewModel.RemindMeLaterButtonLabel;
            downloadButton.StringValue = viewModel.DownloadButtonLabel;
            cancelButton.StringValue = viewModel.CancelButtonLabel;

            messageLabel.StringValue = viewModel.PromptMessage;
        }

        void DownloadItem_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (viewModel.DownloadItem.TotalBytes == 0) {
                progressBar.Indeterminate = true;
                progressBar.DoubleValue = 0;
                progressBar.StartAnimation (this);
                progressLabel.StringValue = Catalog.GetString ("Connecting…");
                return;
            }

            progressBar.StopAnimation (this);
            progressBar.Indeterminate = false;
            progressBar.DoubleValue = viewModel.DownloadItem.Progress;

            progressLabel.StringValue = Catalog.Format (Catalog.GetString (
                "{0} of {1} ({2})",
                comment: "{0} and {1} are sizes, {2} is a percent (e.g. '1.2MB of 6MB (20%)')"),
                byteFormatter.Format ((long)viewModel.DownloadItem.ProgressBytes),
                byteFormatter.Format ((long)viewModel.DownloadItem.TotalBytes),
                percentFormatter.StringFromNumber (new NSNumber (viewModel.DownloadItem.Progress)));
        }
    }
}