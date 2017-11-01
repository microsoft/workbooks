//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.Core
{
    public class AgentStartOptions
    {
        internal ClientSessionKind? ClientSessionKind { get; set; }
        public ushort? Port { get; set; }
    }
}