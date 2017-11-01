//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Telemetry
{
    interface IDataEvent : IEvent
    {
        Task SerializePropertiesAsync (JsonTextWriter writer);
    }
}