//
// DiagnosticExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.Compilation.Roslyn
{
	static class DiagnosticExtensions
	{
		public static FilteredDiagnostics Filter (
			this IReadOnlyList<Diagnostic> diagnostics,
			int maxCount = 5)
			=> new FilteredDiagnostics (diagnostics, maxCount);
	}
}