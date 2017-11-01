//
// TestMessageService.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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