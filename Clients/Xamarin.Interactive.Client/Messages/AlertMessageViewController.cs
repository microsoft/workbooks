//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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