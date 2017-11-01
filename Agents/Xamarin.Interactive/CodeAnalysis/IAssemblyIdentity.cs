//
// IAssemblyIdentity.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
	public interface IAssemblyIdentity : IEquatable<IAssemblyIdentity>
	{
		string Name { get; }
		string FullName { get; }
		Version Version { get; }
	}
}