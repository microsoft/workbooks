//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client.Windows
{
    sealed class WpfMessageViewDelegate : IStatusMessageViewDelegate, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        MessageAction topMessageAction;
        Message topMessage;

        public WpfMessageViewDelegate (Window window)
        {
            window.CommandBindings.Add (new CommandBinding (
                ActionCommand,
                ActionCommandExecute));
        }

        public RoutedUICommand ActionCommand { get; } = new RoutedUICommand();

        void ActionCommandExecute(object sender, RoutedEventArgs args)
        {
            if (topMessage != null && topMessageAction != null)
                topMessage.ActionResponseHandler(topMessage, topMessageAction);
        }

        string statusBarText;
        public string StatusBarText {
            get { return statusBarText; }
            set {
                if (statusBarText != value) {
                    statusBarText = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        string statusBarDetailedText;
        public string StatusBarDetailedText {
            get { return statusBarDetailedText; }
            set {
                if (statusBarDetailedText != value) {
                    statusBarDetailedText = String.IsNullOrEmpty (value) ? null : value;
                    NotifyPropertyChanged ();
                }
            }
        }

        bool isStatusBarVisible;

        public bool IsStatusBarVisible {
            get { return isStatusBarVisible; }
            set {
                if (isStatusBarVisible != value) {
                    isStatusBarVisible = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        bool isActionButtonVisible;

        public bool IsActionButtonVisible {
            get { return isActionButtonVisible; }
            set {
                if (isActionButtonVisible != value) {
                    isActionButtonVisible = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        bool isSpinning;
        public bool IsSpinning {
            get { return isSpinning; }
            set {
                if (isSpinning != value) {
                    isSpinning = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        bool IStatusMessageViewDelegate.CanDisplayMessage (Message message)
        {
            switch (message.Kind) {
            case MessageKind.Status:
                return true;
            case MessageKind.Alert:
                if (message.Actions.Length != 1)
                    return false;

                switch (message.AffirmativeAction?.Id) {
                case MessageAction.DismissActionId:
                case MessageAction.RetryActionId:
                    return true;
                }

                return false;
            }

            return false;
        }

        void IStatusMessageViewDelegate.StartSpinner ()
            => IsSpinning = true;

        void IStatusMessageViewDelegate.StopSpinner ()
            => IsSpinning = false;

        void IStatusMessageViewDelegate.DisplayMessage (Message message)
        {
            topMessageAction = message.Kind == MessageKind.Alert ? message.AffirmativeAction : null;
            topMessage = message;

            StatusBarText = message.Text;
            StatusBarDetailedText = message.DetailedText;
            IsStatusBarVisible = true;

            if (topMessageAction != null) {
                ActionCommand.Text = topMessageAction.Label;
                // Fake property change notification to get button text updated
                NotifyPropertyChanged (nameof (ActionCommand));
                IsActionButtonVisible = true;
            } else
                IsActionButtonVisible = false;
        }

        void IStatusMessageViewDelegate.DisplayIdle ()
        {
            topMessageAction = null;
            topMessage = null;

            StatusBarText = Catalog.GetString ("Ready");
            StatusBarDetailedText = null;
            IsStatusBarVisible = false;
            IsActionButtonVisible = false;
        }
    }
}