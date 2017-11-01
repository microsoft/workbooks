//
// AssemblyDefinition.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Xamarin Inc. All rights reserved.

using System;
using System.Reflection;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis
{
	[Serializable]
	sealed class AssemblyDefinition : IAssemblyDefinition
	{
		IAssemblyIdentity IAssemblyDefinition.Identity => Name;
		IAssemblyContent IAssemblyDefinition.Content => Content;
		IAssemblyEntryPoint IAssemblyDefinition.EntryPoint => EntryPoint;

		public RepresentedAssemblyName Name { get; }
		public AssemblyEntryPoint EntryPoint { get; }
		public AssemblyContent Content { get; }
		public AssemblyDependency [] ExternalDependencies { get; }
		public bool HasIntegration { get; }

		public AssemblyDefinition (
			AssemblyName name,
			FilePath location,
			string entryPointType = null,
			string entryPointMethod = null,
			byte [] peImage = null,
			byte [] debugSymbols = null,
			AssemblyDependency [] externalDependencies = null,
			bool hasIntegration = false)
		{
			Name = new RepresentedAssemblyName (name);
			EntryPoint = new AssemblyEntryPoint (entryPointType, entryPointMethod);
			Content = new AssemblyContent (location, peImage, debugSymbols);
			ExternalDependencies = externalDependencies;
			HasIntegration = hasIntegration;
		}
	}
}