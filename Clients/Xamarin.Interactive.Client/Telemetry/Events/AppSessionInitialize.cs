//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Telemetry.Events
{
    sealed class AppSessionInitialize : Event
    {
        public AppSessionInitialize () : base ("app.sessionInit")
        {
        }
    }
}