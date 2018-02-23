//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Client.Web
{
    struct ClientConnectionId : IEquatable<ClientConnectionId>
    {
        readonly string id;

        public ClientConnectionId (string id)
            => this.id = id ?? throw new ArgumentNullException (nameof (id));

        public bool Equals (ClientConnectionId other)
            => other.id == id;

        public override bool Equals (object obj)
            => obj is ClientConnectionId ccid && Equals (ccid);

        public override int GetHashCode()
            => id.GetHashCode ();

        public override string ToString ()
            => id;

        public static implicit operator string (ClientConnectionId ccid)
            => ccid.id;

        public static implicit operator ClientConnectionId (string id)
            => new ClientConnectionId (id);
    }
}