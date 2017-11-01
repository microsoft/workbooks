//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Client.Updater
{
    sealed class DamagedDownloadException : Exception
    {
        public DamagedDownloadException (string message) : base (message)
        {
        }
    }
}