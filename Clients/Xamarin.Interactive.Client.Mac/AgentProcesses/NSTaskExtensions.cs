//
// NSTaskExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Foundation;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    static class NSTaskExtensions
    {
        public static NSObject NotifyTerminated (
            this NSTask task,
            NSNotificationCenter notificationCenter,
            Action<NSTask> handler)
        {
            if (task == null)
                throw new ArgumentNullException (nameof (task));

            if (handler == null)
                throw new ArgumentNullException (nameof (handler));

            NSObject observer = null;

            observer = notificationCenter.AddObserver (
                NSTask.NSTaskDidTerminateNotification,
                notification => {
                    if (notification.Object == task) {
                        notificationCenter.RemoveObserver (observer);
                        handler (task);
                    }
                });

            return observer;
        }
    }
}