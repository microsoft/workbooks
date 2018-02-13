//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace System.Windows.Input
{
    interface IRoutedCommandFactory
    {
        ICommand CreateRoutedUICommand (string text, string name, Type ownerType);
    }
}