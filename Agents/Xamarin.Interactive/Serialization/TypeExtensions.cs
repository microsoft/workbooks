//
// TypeExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Serialization
{
	static class TypeExtensions
	{
		/// <summary>
		/// Gets the name of the type without any assembly qualification, including
		/// on generic type arguments. No assembly qualification allows for redirecting
		/// types from one assembly to another when serializing.
		/// </summary>
		/// <remarks>
		/// We do *not* want to assembly qualify types since the agent assembly
		/// and the client assembly, which have the same types, will have different
		/// assembly names
		///
		/// Type.AssemblyQualifiedName: fully qualifies (X`1[[Y,asm2]],asm1) (bad)
		/// Type.FullName: fully qualifies generic type arguments (X`1[[Y,asm2]]) (bad)
		/// Type.ToString: does not qualify type or generic type arguments (X`1[Y]) (good)
		/// </remarks>
		public static string ToSerializableName (this Type type)
			=> type?.ToString ();
	}
}