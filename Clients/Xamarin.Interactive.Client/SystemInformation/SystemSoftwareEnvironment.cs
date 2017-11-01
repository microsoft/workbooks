//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Xamarin.Interactive.SystemInformation
{
    sealed class SystemSoftwareEnvironment : ISoftwareEnvironment
    {
        public string Name { get; } = "system";

        readonly List<ISoftwareComponent> components = new List<ISoftwareComponent> ();

        public void Add (ISoftwareComponent component)
            => components.Add (component ?? throw new ArgumentNullException (nameof (component)));

        public IEnumerator<ISoftwareComponent> GetEnumerator ()
            => components.GetEnumerator ();

        IEnumerator IEnumerable.GetEnumerator ()
            => GetEnumerator ();
    }
}