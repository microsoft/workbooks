//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.Core
{
    sealed class AgentStartOptions
    {
        internal ClientSessionKind? ClientSessionKind { get; set; }

        public ushort? Port { get; set; }

        public Action<string> AgentStarted { get; set; }
    }
}