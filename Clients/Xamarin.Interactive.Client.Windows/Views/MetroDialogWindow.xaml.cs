//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Windows;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Xamarin.Interactive.Client.Windows.Views
{
    partial class MetroDialogWindow : MetroWindow
    {
        public static readonly DependencyProperty MaxContentWidthProperty = DependencyProperty.Register (
            "MaxContentWidth", typeof (int), typeof (MetroDialogWindow), new PropertyMetadata (300));
        public int MaxContentWidth {
            get { return (int)GetValue (MaxContentWidthProperty); }
            set { SetValue (MaxContentWidthProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register (
            "Message", typeof (string), typeof (MetroDialogWindow), new UIPropertyMetadata (null));
        public string Message {
            get { return (string)GetValue (MessageProperty); }
            set { SetValue (MessageProperty, value); }
        }

        public static readonly DependencyProperty AffirmativeButtonTextProperty = DependencyProperty.Register (
            "AffirmativeButtonText", typeof (string), typeof (MetroDialogWindow), new FrameworkPropertyMetadata ("OK"));
        public string AffirmativeButtonText {
            get { return (string)GetValue (AffirmativeButtonTextProperty); }
            set { SetValue (AffirmativeButtonTextProperty, value); }
        }

        public static readonly DependencyProperty NegativeButtonTextProperty = DependencyProperty.Register (
            "NegativeButtonText", typeof (string), typeof (MetroDialogWindow), new FrameworkPropertyMetadata ("Cancel"));
        public string NegativeButtonText {
            get { return (string)GetValue (NegativeButtonTextProperty); }
            set { SetValue (NegativeButtonTextProperty, value); }
        }

        public static readonly DependencyProperty FirstAuxiliaryButtonTextProperty = DependencyProperty.Register (
            "FirstAuxiliaryButtonText", typeof (string), typeof (MetroDialogWindow), new PropertyMetadata ("Cancel"));
        public string FirstAuxiliaryButtonText {
            get { return (string)GetValue (FirstAuxiliaryButtonTextProperty); }
            set { SetValue (FirstAuxiliaryButtonTextProperty, value); }
        }

        public static readonly DependencyProperty SecondAuxiliaryButtonTextProperty = DependencyProperty.Register (
            "SecondAuxiliaryButtonText", typeof (string), typeof (MetroDialogWindow), new PropertyMetadata ("Cancel"));
        public string SecondAuxiliaryButtonText {
            get { return (string)GetValue (SecondAuxiliaryButtonTextProperty); }
            set { SetValue (SecondAuxiliaryButtonTextProperty, value); }
        }

        public static readonly DependencyProperty ButtonStyleProperty = DependencyProperty.Register (
            "ButtonStyle", typeof (MessageDialogStyle), typeof (MetroDialogWindow),
            new PropertyMetadata (MessageDialogStyle.Affirmative, (o, args) => {
                ((MetroDialogWindow)o).UpdateButtonState ();
            }));
        public MessageDialogStyle ButtonStyle {
            get { return (MessageDialogStyle)GetValue (ButtonStyleProperty); }
            set { SetValue (ButtonStyleProperty, value); }
        }

        public MessageDialogResult Result { get; private set; } = MessageDialogResult.FirstAuxiliary;

        public MetroDialogWindow ()
        {
            InitializeComponent ();

            DataContext = this;
        }

        void UpdateButtonState ()
        {
            if (AffirmativeButton == null)
                return;

            if (ButtonStyle == MessageDialogStyle.Affirmative) {
                AffirmativeButton.Visibility = Visibility.Visible;
                NegativeButton.Visibility = Visibility.Collapsed;
                FirstAuxiliaryButton.Visibility = Visibility.Collapsed;
                SecondAuxiliaryButton.Visibility = Visibility.Collapsed;
            }
            else {
                NegativeButton.Visibility = Visibility.Visible;

                FirstAuxiliaryButton.Visibility =
                    (ButtonStyle == MessageDialogStyle.AffirmativeAndNegativeAndDoubleAuxiliary ||
                     ButtonStyle == MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary)
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                SecondAuxiliaryButton.Visibility =
                    ButtonStyle == MessageDialogStyle.AffirmativeAndNegativeAndDoubleAuxiliary
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }
        }

        void OnButtonClick (object sender, RoutedEventArgs e)
        {
            if (e.Source == AffirmativeButton)
                Result = MessageDialogResult.Affirmative;
            else if (e.Source == NegativeButton)
                Result = MessageDialogResult.Negative;
            else if (e.Source == FirstAuxiliaryButton)
                Result = MessageDialogResult.FirstAuxiliary;
            else if (e.Source == SecondAuxiliaryButton)
                Result = MessageDialogResult.SecondAuxiliary;

            Close ();
        }
    }
}
