//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;

namespace Xamarin.Interactive.Messages
{
    sealed class StatusMessageViewController : IMessageService
    {
        readonly IStatusMessageViewDelegate viewDelegate;

        ImmutableList<Message> messages = ImmutableList<Message>.Empty;
        int spinCount;

        public StatusMessageViewController (IStatusMessageViewDelegate viewDelegate)
        {
            if (viewDelegate == null)
                throw new ArgumentNullException (nameof (viewDelegate));

            this.viewDelegate = viewDelegate;
        }

        public bool CanHandleMessage (Message message)
            => viewDelegate.CanDisplayMessage (message);

        public Message PushMessage (Message message)
        {
            if (message == null)
                throw new ArgumentNullException (nameof (message));

            message = message.WithMessageService (this);

            MainThread.Post (() => {
                messages = messages.Add (message);

                if (message.ShowSpinner) {
                    spinCount++;
                    if (spinCount == 1)
                        viewDelegate.StartSpinner ();
                }

                viewDelegate.DisplayMessage (message);
            });

            return message;
        }

        public void DismissAll ()
            => messages.ForEach (m => m.Dispose ());

        public void DismissMessage (int messageId)
        {
            MainThread.Post (() => {
                var messageIndex = messages.FindLastIndex (m => m.Id == messageId);
                if (messageIndex < 0)
                    return;

                var message = messages [messageIndex];

                messages = messages.RemoveAt (messageIndex);

                if (message.ShowSpinner) {
                    spinCount--;
                    if (spinCount == 0)
                        viewDelegate.StopSpinner ();
                }

                if (messages.Count > 0)
                    viewDelegate.DisplayMessage (messages [messages.Count - 1]);
                else
                    viewDelegate.DisplayIdle ();
            });
        }
    }
}