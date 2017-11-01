//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AppKit;
using Foundation;
using ObjCRuntime;

namespace System.Windows.Input
{
    public sealed class RoutedUICommandMenuItem : NSMenuItem
    {
        static readonly Selector executeSel = new Selector ("execute:");

        readonly RoutedUICommand command;
        readonly object parameter;

        public RoutedUICommandMenuItem (RoutedUICommand command, object parameter)
        {
            if (command == null)
                throw new ArgumentNullException (nameof (command));

            this.command = command;
            this.parameter = parameter;

            Title = command.Text;

            Target = this;
            Action = executeSel;
        }

        public override bool Enabled {
            get { return command.CanExecute (parameter, this); }
            set { }
        }

        [Export ("execute:")]
        void Execute (NSObject sender)
            => command.Execute (parameter, this);
    }
}