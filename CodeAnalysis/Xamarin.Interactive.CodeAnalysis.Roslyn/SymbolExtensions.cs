//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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