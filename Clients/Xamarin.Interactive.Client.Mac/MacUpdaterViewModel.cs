//
// MacUpdaterViewModel.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;

using AppKit;
using Foundation;

using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Client.Mac
{
    sealed class MacUpdaterViewModel : UpdaterViewModel
    {
        readonly NSWindow window;

        public MacUpdaterViewModel (NSWindow window, UpdateItem updateItem)
            : base ((NSString)NSBundle.MainBundle.InfoDictionary ["CFBundleName"], updateItem)
        {
            this.window = window;
        }

        protected override Task InstallUpdateAsync ()
        {
            window.Close ();
            NSWorkspace.SharedWorkspace.OpenFile (DownloadItem.TargetFile);
            return Task.CompletedTask;
        }

        protected override void RunErrorDialog (bool isDownloadError, string message)
        {
            var alert = new NSAlert {
                AlertStyle = NSAlertStyle.Critical,
                MessageText = isDownloadError
                    ? Catalog.GetString ("Error Downloading Update")
                    : Catalog.GetString ("Error Installing Update"),
                InformativeText = message
            };

            if (isDownloadError)
                alert.AddButton (Catalog.GetString ("Download & Install Manually"));

            alert.AddButton (Catalog.GetString ("Dismiss"));

            alert.BeginSheet (window, response => {
                if (isDownloadError && (int)response == 1000)
                    NSWorkspace.SharedWorkspace.OpenUrl (UpdateItem.DownloadUrl);
            });
        }

        public static void PresentUpdateCheckFailedDialog (Exception e)
            => e.ToUserPresentable (Catalog.GetString ("An error occurred when checking for updates."))
                .Present ();

        public static void PresentUpToDateDialog ()
        {
            var visualStudio = VisualStudioForMacComponent.Installation;

            var alert = new NSAlert {
                AlertStyle = NSAlertStyle.Informational,
                MessageText = Catalog.Format (Catalog.GetString (
                    "{0} is up to date!",
                    comment: "{0} is the application name"),
                    ClientInfo.FullProductName),
                InformativeText = Catalog.Format (Catalog.GetString (
                    "{0} is the latest version available in the Xamarin " +
                    "{1} channel, released on {2}.\n\n" +
                    "You can switch update channels in " +
                    "{3} under the “Check for Updates” main menu item.",
                    comment: "{0} is a version number; {1} is a channel; " +
                        "{2} is a localized date; " +
                        "{3} is Xamarin Studio or Visual Studio"),
                    BuildInfo.Version.ToString (Versioning.ReleaseVersionFormat.FriendlyShort),
                    visualStudio.UpdaterChannel,
                    BuildInfo.Date.ToString ("D"),
                    visualStudio.Name)
            };

            alert.AddButton (Catalog.GetString ("OK"));
            alert.AddButton (Catalog.Format (Catalog.GetString (
                "Open {0}",
                comment: "{0} is an app name"),
                visualStudio.Name));

            if (alert.RunModal () == 1000)
                return;

            try {
                visualStudio.Launch ();
            } catch (Exception e) {
                e.ToUserPresentable (Catalog.Format (Catalog.GetString (
                    "Unable to launch {0}."),
                    visualStudio.Name
                )).Present ();
            }
        }
    }
}