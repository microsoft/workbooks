//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Windows.Input;

namespace Xamarin.Interactive.Client.Windows.Commands
{
    static class Commands
    {
        public static readonly ICommand About = new AboutCommand ();

        public static readonly ICommand Help = new HelpCommand ();

        public static readonly ICommand CheckForUpdates = new CheckForUpdatesCommand ();

        public static readonly ICommand ShowOptions = new ShowOptionsCommand ();

        public static readonly ICommand OpenFile = new OpenFileCommand ();

        public static readonly ICommand ClearRecentFiles = new ClearRecentFilesCommand ();

        public static readonly ICommand OpenSampleWorkbooks = new OpenSampleWorkbooksCommand ();

        public static readonly ICommand CloseWindow = new CloseWindowCommand ();

        public static readonly RoutedUICommand IncreaseFont =
            new RoutedUICommand ("Increase Font Size", "IncreaseFont", typeof (Commands));
        public static readonly RoutedUICommand DecreaseFont =
            new RoutedUICommand ("Decrease Font Size", "DecreaseFont", typeof (Commands));
        public static readonly RoutedUICommand ResetFontSize =
            new RoutedUICommand ("Reset Font Size", "ResetFontSize", typeof (Commands));

        public static readonly RoutedUICommand ClearHistory =
            new RoutedUICommand ("Clear History", "ClearHistory", typeof (Commands));

        public static readonly RoutedUICommand ExecuteAll =
            new RoutedUICommand ("Run All", "ExecuteAll", typeof (Commands),
                new InputGestureCollection { new KeyGesture (Key.R, ModifierKeys.Control) });

        public static readonly RoutedUICommand RemovePackage =
            new RoutedUICommand ("Remove Package", "RemovePackage", typeof (Commands));
    }
}
