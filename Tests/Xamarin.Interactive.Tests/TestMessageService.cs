//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Tests
{
    sealed class TestMessageService : IMessageService
    {
        public bool CanHandleMessage (Message message)
            => true;

        public void DismissMessage (int messageId)
        {
        }

        public Message PushMessage (Message message)
        {
            Log.Info (nameof (TestMessageService), message.Text);
            return message;
        }
    }
}