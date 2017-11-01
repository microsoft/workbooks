//
// AssemblyEntryPoint.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
	[Serializable]
	sealed class AssemblyEntryPoint : IAssemblyEntryPoint
	{
		public string TypeName { get; }
		public string MethodName { get; }

		public AssemblyEntryPoint (string typeName, string methodName)
		{
			TypeName = typeName;
			MethodName = methodName;
		}
	}
}