//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Telemetry.Models
{
    // Keep in sync with ClientSessionKind
    enum AgentSessionKind
    {
        Unknown,
        Workbook,
        LiveInspection
    }
}