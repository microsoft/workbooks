//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Client.ViewControllers;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Client.Console
{
    sealed class ConsoleClientSessionViewControllers : IClientSessionViewControllers
    {
        public MessageViewController Messages { get; }
            = new MessageViewController (
                new StatusMessageViewDelegate (),
                new AlertMessageViewDelegate ());

        public History ReplHistory { get; }
            = new History (
                Array.Empty<string> (),
                persist: false);

        public WorkbookTargetsViewController WorkbookTargets { get; }
            = new WorkbookTargetsViewController ();

        sealed class StatusMessageViewDelegate : IStatusMessageViewDelegate
        {
            public bool CanDisplayMessage (Message message) => true;

            public void DisplayIdle ()
            {
            }

            public void DisplayMessage (Message message)
            {
                try {
                    System.Console.ForegroundColor = ConsoleColor.Magenta;
                    System.Console.Error.WriteLine (message.Text);
                } finally {
                    System.Console.ResetColor ();
                }
            }

            public void StartSpinner ()
            {
            }

            public void StopSpinner ()
            {
            }
        }

        sealed class AlertMessageViewDelegate : IAlertMessageViewDelegate
        {
            public void DismissMessage (int messageId)
            {
            }

            public void DisplayMessage (Message message)
            {
            }
        }
    }
}