//
// NSAlertMessageViewDelegate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using AppKit;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client.Mac
{
    sealed class NSAlertMessageViewDelegate : IAlertMessageViewDelegate
    {
        readonly NSWindow window;
        readonly Dictionary<int, NSAlert> alerts = new Dictionary<int, NSAlert> ();

        public NSAlertMessageViewDelegate (NSWindow window)
        {
            if (window == null)
                throw new ArgumentNullException (nameof (window));

            this.window = window;
        }

        public void DismissMessage (int messageId)
        {
            NSAlert alert;
            if (alerts.TryGetValue (messageId, out alert)) {
                alerts.Remove (messageId);

                window.EndSheet (alert.Window);
                alert.Window.OrderOut (null);
                alert.Dispose ();
            }
        }

        public void DisplayMessage (Message message)
        {
            var alert = new NSAlert ();

            alerts.Add (message.Id, alert);

            switch (message.Severity) {
            case MessageSeverity.Error:
                alert.AlertStyle = NSAlertStyle.Critical;
                break;
            default:
                alert.AlertStyle = NSAlertStyle.Informational;
                break;
            }

            if (!String.IsNullOrEmpty (message.Text))
                alert.MessageText = message.Text;

            if (!String.IsNullOrEmpty (message.DetailedText))
                alert.InformativeText = message.DetailedText;

            Action<MessageAction> addButton = action => {
                if (action == null)
                    return;

                var button = alert.AddButton (action.Label);
                if (action.Tooltip != null)
                    button.ToolTip = action.Tooltip;

                button.Activated += (sender, e) => message.ActionResponseHandler (message, action);
            };

            addButton (message.AffirmativeAction);
            addButton (message.NegativeAction);
            addButton (message.AuxiliaryAction);

            alert.BeginSheet (window);
        }
    }
}