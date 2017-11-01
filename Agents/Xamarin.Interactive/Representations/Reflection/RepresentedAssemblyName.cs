//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.Serialization;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations.Reflection
{
    [Serializable]
    sealed class RepresentedAssemblyName : IAssemblyIdentity, ISerializable
    {
        readonly AssemblyName assemblyName;

        public string Name => assemblyName.Name;
        public string FullName => assemblyName.FullName;
        public Version Version => assemblyName.Version;

        public RepresentedAssemblyName (AssemblyName assemblyName)
            => this.assemblyName = assemblyName
                ?? throw new ArgumentNullException (nameof (assemblyName));

        internal RepresentedAssemblyName (SerializationInfo info, StreamingContext context)
            => assemblyName = new AssemblyName (info.GetValue<string> ("AssemblyName"));

        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context)
            => info.AddValue ("AssemblyName", assemblyName.FullName);

        public bool Equals (IAssemblyIdentity other)
            => ReferenceEquals (other, this) || FullName.Equals (other.FullName);

        public override bool Equals (object obj)
            => obj is IAssemblyIdentity identity && Equals (identity);

        public override int GetHashCode ()
            => FullName.GetHashCode ();

        public override string ToString ()
            => FullName;

        public static implicit operator AssemblyName (RepresentedAssemblyName representedAssemblyName)
            => representedAssemblyName.assemblyName;
    }
}