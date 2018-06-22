//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.Telemetry.Models
{
    sealed class AppSession : ITelemetryEvent
    {
        public string UpdateChannel { get; set; }

        Dictionary<string, string> ITelemetryEvent.GetProperties ()
        {
            var dict = new Dictionary<string, string> ();

            if (!string.IsNullOrEmpty (UpdateChannel))
                dict.Add (nameof (UpdateChannel), UpdateChannel);

            return dict;
        }
    }
}