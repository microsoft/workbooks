//
// IAssemblyDefinition.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace Xamarin.Interactive.CodeAnalysis
{
	/// <summary>
	/// Represents a complete assembly as loaded in the agent.
	/// </summary>
	public interface IAssemblyDefinition
	{
		IAssemblyIdentity Identity { get; }
		IAssemblyContent Content { get; }
		IAssemblyEntryPoint EntryPoint { get; }
	}
}