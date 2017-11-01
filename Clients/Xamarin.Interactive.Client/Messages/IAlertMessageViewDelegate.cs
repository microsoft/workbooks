//
// IAlertMessageViewDelegate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Messages
{
    interface IAlertMessageViewDelegate
    {
        void DisplayMessage (Message message);
        void DismissMessage (int messageId);
    }
}