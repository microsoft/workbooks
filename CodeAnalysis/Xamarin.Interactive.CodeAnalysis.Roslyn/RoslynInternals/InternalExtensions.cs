//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn.Internals
{
    static class InternalExtensions
    {
        static readonly Type symbolExtensionsType = typeof (Workspace).Assembly.GetType (
            "Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions");
        static readonly Type semanticModelExtensionsType = typeof (Workspace).Assembly.GetType (
            "Microsoft.CodeAnalysis.Shared.Extensions.SemanticModelExtensions");

        static readonly MethodInfo isAccessibleWithin = symbolExtensionsType.GetMethod (
            "IsAccessibleWithin",
            new [] {
                typeof (ISymbol),
                typeof (ISymbol),
                typeof (ITypeSymbol)
            });
        static readonly MethodInfo getSymbolType = symbolExtensionsType.GetMethod (
            "GetSymbolType",
            new [] { typeof (ISymbol) });
        static readonly MethodInfo getEnclosingNamedTypeOrAssembly = semanticModelExtensionsType.GetMethod (
            "GetEnclosingNamedTypeOrAssembly",
            new [] {
                typeof (SemanticModel),
                typeof (int),
                typeof (CancellationToken)
            });

        public static bool IsAccessibleWithin (
            this ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughTypeOpt = null)
            => (bool)isAccessibleWithin.Invoke (
                null,
                new object [] { symbol, within, throughTypeOpt });

        public static ITypeSymbol GetSymbolType (this ISymbol symbol)
            => (ITypeSymbol)getSymbolType.Invoke (null, new object [] { symbol });

        public static ISymbol GetEnclosingNamedTypeOrAssembly (
            this SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken)
            => (ISymbol)getEnclosingNamedTypeOrAssembly.Invoke (
                null,
                new object [] { semanticModel, position, cancellationToken });
    }
}
