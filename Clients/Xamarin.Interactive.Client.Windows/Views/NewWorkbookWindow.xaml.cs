//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Client.ViewControllers;

namespace Xamarin.Interactive.Client.Windows.Views
{
    partial class NewWorkbookWindow
    {
        const string TAG = nameof (NewWorkbookWindow);

        readonly MenuManager menuManager;
        readonly NewWorkbookViewController viewController;

        public AgentType SelectedAgentType {
            get => viewController.SelectedAgentType;
            set => viewController.SelectedAgentType = value;
        }

        public NewWorkbookWindow ()
        {
            InitializeComponent ();

            menuManager = new MenuManager (mainMenu, this);
            viewController = new NewWorkbookViewController ();

            DataContext = viewController;

            Loaded += (o, e) =>
                sideImage.Sources = new [] {
                    new BitmapImage (new Uri ("pack://application:,,,/new-workbook-background.png")),
                    new BitmapImage (new Uri ("pack://application:,,,/new-workbook-background@2x.png")),
                };
        }

        void OnCreateWorkbook (object sender, RoutedEventArgs e)
        {
            var agentType = viewController.SelectedAgentType;

            AgentSessionWindow window = null;
            try {
                window = AgentSessionWindow.Open (viewController.SelectedItem.CreateClientSessionUri ());
            } catch (Exception ex) {
                Log.Error (TAG, ex);
            }

            if (window != null) {
                // Without this, the WPF agent app window remains in the background.
                window.Activate ();
                Hide ();
            }
        }

        protected override void OnClosing (CancelEventArgs e)
        {
            e.Cancel = true;
            Hide ();
            App.CheckNeedsExit ();
        }

        void OnCloseCommandExecuted (object sender, ExecutedRoutedEventArgs e)
            => Close ();

        void OnOpenCommandExecuted (object sender, ExecutedRoutedEventArgs e)
            => App.OpenWorkbook ();
    }
}