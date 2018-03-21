// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [Serializable]
    public sealed class AssemblyIdentity
    {
        public string Name { get; }
        public string FullName { get; }
        public Version Version { get; }

        internal AssemblyIdentity (AssemblyName assemblyName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException (nameof (assemblyName));

            Name = assemblyName.Name;
            FullName = assemblyName.FullName;
            Version = assemblyName.Version;
        }

        public bool Equals (AssemblyIdentity other)
            => ReferenceEquals (other, this) || FullName.Equals (other.FullName);

        public override bool Equals (object obj)
            => obj is AssemblyIdentity identity && Equals (identity);

        public override int GetHashCode ()
            => FullName.GetHashCode ();

        public override string ToString ()
            => FullName;

        public static implicit operator AssemblyName (AssemblyIdentity assemblyIdentity)
            => new AssemblyName (assemblyIdentity.FullName);
    }
}