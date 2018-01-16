//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Telemetry.Models
{
    sealed class OperatingSystem : ITelemetryEvent
    {
        public Guid OperatingSystemId { get; set; }
        public OperatingSystemName Name { get; set; }
        public string Version { get; set; }
        public string VersionMetadata { get; set; }
        public byte WordSize { get; set; }
        public byte CpuWordSize { get; set; }
    }
}