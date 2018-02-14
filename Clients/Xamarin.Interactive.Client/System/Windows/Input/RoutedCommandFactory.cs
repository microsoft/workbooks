//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace System.Windows.Input
{
    static class RoutedCommandFactory
    {
        static IRoutedCommandFactory instance = new NopRoutedCommandFactory ();

        public static void Initialize (IRoutedCommandFactory routedCommandFactory)
            => instance = routedCommandFactory ?? throw new ArgumentNullException (nameof (routedCommandFactory));

        public static ICommand CreateRoutedUICommand (string text, string name, Type ownerType)
            => instance.CreateRoutedUICommand (text, name, ownerType);

        sealed class NopRoutedCommandFactory : IRoutedCommandFactory
        {
            ICommand IRoutedCommandFactory.CreateRoutedUICommand (string text, string name, Type ownerType)
                => NopCommand.Instance;

            sealed class NopCommand : ICommand
            {
                public static readonly NopCommand Instance = new NopCommand ();

                NopCommand ()
                {
                }

                event EventHandler ICommand.CanExecuteChanged {
                    add { }
                    remove { }
                }

                public bool CanExecute (object parameter)
                    => false;

                public void Execute (object parameter)
                    => throw new NotImplementedException ();
            }
        }
    }
}