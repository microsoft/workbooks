//
// TargetCompilationConfiguration.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
	[Serializable]
	sealed class TargetCompilationConfiguration
	{
		public string GlobalStateTypeName { get; set; }
		public AssemblyDefinition GlobalStateAssembly { get; set; }
		public string[] DefaultUsings { get; set; }
		public int EvaluationContextId { get; set; }
	}
}