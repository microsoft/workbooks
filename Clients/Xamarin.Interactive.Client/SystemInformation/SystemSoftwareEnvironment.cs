//
// SystemSoftwareEnvironment.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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