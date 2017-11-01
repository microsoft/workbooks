//
// SymbolExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.Compilation.Roslyn
{
	static class SymbolExtensions
	{
		public static bool IsNonGenericTaskType (this ITypeSymbol symbol)
			=> symbol != null && symbol.ToDisplayString (
				SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Threading.Tasks.Task";
	}
}