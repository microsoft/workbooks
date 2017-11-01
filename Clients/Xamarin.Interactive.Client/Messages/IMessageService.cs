//
// IMessageService.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Messages
{
    interface IMessageService
    {
        bool CanHandleMessage (Message message);
        Message PushMessage (Message message);
        void DismissMessage (int messageId);
    }
}