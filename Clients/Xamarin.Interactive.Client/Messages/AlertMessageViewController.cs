//
// AlertMessageViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Messages
{
    sealed class AlertMessageViewController : IMessageService
    {
        readonly IAlertMessageViewDelegate viewDelegate;

        public AlertMessageViewController (IAlertMessageViewDelegate viewDelegate)
        {
            if (viewDelegate == null)
                throw new ArgumentNullException (nameof (viewDelegate));

            this.viewDelegate = viewDelegate;
        }

        public bool CanHandleMessage (Message message)
            => message.Kind == MessageKind.Alert;

        public Message PushMessage (Message message)
        {
            if (message == null)
                throw new ArgumentNullException (nameof (message));

            message = message.WithMessageService (this);

            MainThread.Post (() => viewDelegate.DisplayMessage (message));

            return message;
        }

        public void DismissMessage (int messageId)
            => MainThread.Post (() => viewDelegate.DismissMessage (messageId));
    }
}