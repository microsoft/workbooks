//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.Interactive.SystemInformation
{
    interface ISoftwareComponent
    {
        string Name { get; }
        string Version { get; }
        bool IsInstalled { get; }

        void SerializeExtraProperties (JsonTextWriter writer);
    }
}