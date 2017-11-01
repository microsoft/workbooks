//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Windows;

using Xamarin.Interactive.Client.Windows.Views;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Windows
{
    static class UserPresentableExceptionHandler
    {
        const string TAG = nameof (UserPresentableExceptionHandler);

        public static void Present (this UserPresentableException e, object uiContext = null)
        {
            Log.Error (TAG, $"{e.Message} ({e.Details})", e.InnerException ?? e);

            new Telemetry.Events.Error (e).Post ();

            MainThread.Ensure ();

            var window = new MetroDialogWindow {
                Title = e.Message,
                Message = e.Details,
                AffirmativeButtonText = Catalog.GetString ("OK")
            };

            var ownerWindow = uiContext as Window;

            if (ownerWindow == null) {
                window.Owner = ownerWindow;
                window.Width = 400;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            } else {
                window.Width = ownerWindow.Width;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            window.ShowDialog ();
        }
    }
}