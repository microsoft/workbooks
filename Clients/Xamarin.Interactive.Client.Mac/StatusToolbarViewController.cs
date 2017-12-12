//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.Client.ViewControllers;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class StatusToolbarViewController : NSViewController, IStatusMessageViewDelegate
    {
        [Register ("StatusToolbarViewImageView")]
        sealed class StatusToolbarViewImageView : NSImageView
        {
            StatusToolbarViewImageView (IntPtr handle) : base (handle)
            {
            }

            public override bool MouseDownCanMoveWindow => true;
        }

        [Register ("StatusToolbarViewProgressIndicator")]
        sealed class StatusToolbarViewProgressIndicator : NSProgressIndicator
        {
            StatusToolbarViewProgressIndicator (IntPtr handle) : base (handle)
            {
            }

            public override bool MouseDownCanMoveWindow => true;
        }

        static readonly NSImage appIconImage = NSImage.ImageNamed ("AppIcon");
        static readonly NSImage cancelImage = NSImage.ImageNamed ("cancel-16");
        static readonly NSImage errorImage = NSImage.ImageNamed ("error-16");
        static readonly NSImage refreshImage = NSImage.ImageNamed ("refresh-16");

        Message topMessage;
        MessageAction topMessageAction;

        public ClientSession Session { get; set; }

        StatusToolbarViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            stackView.DetachesHiddenViews = true;

            indeterminateProgressIndicator.Hidden = true;

            actionButton.Hidden = true;
            actionButton.Image = refreshImage;
            actionButton.Activated += ActionButton_Activated;
        }

        public override void ViewDidAppear ()
        {
            base.ViewDidAppear ();

            Session.Subscribe (evnt => {
                if (evnt.Kind == ClientSessionEventKind.SessionTitleUpdated && topMessage == null)
                    DisplayIdle ();
            });

            DisplayIdle ();
        }

        void ActionButton_Activated (object sender, EventArgs e)
        {
            if (topMessage != null && topMessageAction != null)
                topMessage.ActionResponseHandler (topMessage, topMessageAction);
        }

        void UpdateImage (NSImage image)
            => imageView.Image = image ?? appIconImage;

        public void DisplayIdle ()
        {
            topMessage = null;
            topMessageAction = null;

            UpdateImage (null);

            if (Session.Agent.Type == AgentType.Unknown) {
                textField.StringValue = String.Empty;
                return;
            }

            var paragraphStyle = (NSMutableParagraphStyle)NSParagraphStyle
                .DefaultParagraphStyle
                .MutableCopy ();

            paragraphStyle.LineBreakMode = NSLineBreakMode.TruncatingTail;

            var attributes = new NSStringAttributes {
                Font = textField.Font,
                ForegroundColor = NSColor.HeaderText,
                ParagraphStyle = paragraphStyle
            }.Dictionary;

            var title = new NSMutableAttributedString (Session.Title, attributes);

            if (Session.SecondaryTitle != null) {
                title.Append (new NSAttributedString (" │ ", attributes));
                title.Append (new NSAttributedString (
                    Session.SecondaryTitle,
                    new NSStringAttributes {
                        Font = textField.Font,
                        ForegroundColor = NSColor.DisabledControlText,
                        ParagraphStyle = paragraphStyle
                    }));
            }

            textField.AttributedStringValue = title;
        }

        public bool CanDisplayMessage (Message message)
        {
            switch (message.Kind) {
            case MessageKind.Status:
                return true;
            case MessageKind.Alert:
                if (message.Actions.Count != 1)
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

        public void DisplayMessage (Message message)
        {
            topMessageAction = message.Kind == MessageKind.Alert ? message.AffirmativeAction : null;
            topMessage = message;

            textField.StringValue = message.Text ?? String.Empty;
            textField.ToolTip = message.DetailedText ?? String.Empty;

            switch (message.Severity) {
            case MessageSeverity.Error:
                textField.TextColor = ThemeExtensions.StatusBar.ErrorTextColor;
                UpdateImage (errorImage);
                break;
            default:
                textField.TextColor = ThemeExtensions.StatusBar.InfoTextColor;
                UpdateImage (null);
                break;
            }

            if (topMessageAction != null) {
                actionButton.ToolTip = topMessageAction.Tooltip ?? String.Empty;
                actionButton.Hidden = false;
                switch (topMessageAction.Id) {
                case MessageAction.DismissActionId:
                    actionButton.Image = cancelImage;
                    break;
                case MessageAction.RetryActionId:
                    actionButton.Image = refreshImage;
                    break;
                }
            } else
                actionButton.Hidden = true;
        }

        // The show<->hide spinner transition has a little delay to avoid flickering
        // the spinner for when we have immediately successive status push/pops
        static readonly TimeSpan spinnerTransitionDelay = TimeSpan.FromMilliseconds (200);
        bool showSpinner;

        public void StartSpinner ()
        {
            showSpinner = true;

            Invoke (() => {
                if (showSpinner && indeterminateProgressIndicator.Hidden) {
                    indeterminateProgressIndicator.StartAnimation (this);
                    indeterminateProgressIndicator.Hidden = false;
                }
            }, spinnerTransitionDelay);
        }

        public void StopSpinner ()
        {
            showSpinner = false;

            Invoke (() => {
                if (!showSpinner && !indeterminateProgressIndicator.Hidden) {
                    indeterminateProgressIndicator.StopAnimation (this);
                    indeterminateProgressIndicator.Hidden = true;
                }
            }, spinnerTransitionDelay);
        }
    }
}