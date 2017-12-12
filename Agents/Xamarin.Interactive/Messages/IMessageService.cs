//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Messages
{
    interface IMessageService
    {
        bool CanHandleMessage (Message message);
        Message PushMessage (Message message);
        void DismissMessage (int messageId);
    }
}