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

        #region IsAccessibleWithin

        delegate bool IsAccessibleWithinDelegate (
            ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughTypeOpt);

        static readonly IsAccessibleWithinDelegate isAccessibleWithin =
            (IsAccessibleWithinDelegate)symbolExtensionsType
            .GetMethod (
                nameof (IsAccessibleWithin),
                new [] {
                    typeof (ISymbol),
                    typeof (ISymbol),
                    typeof (ITypeSymbol)
                })
            .CreateDelegate (typeof (IsAccessibleWithinDelegate));

        public static bool IsAccessibleWithin (
            this ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughTypeOpt = null)
            => isAccessibleWithin (symbol, within, throughTypeOpt);

        #endregion

        #region GetSymbolType

        delegate ITypeSymbol GetSymbolTypeDelegate (ISymbol symbol);

        static readonly GetSymbolTypeDelegate getSymbolType =
            (GetSymbolTypeDelegate)symbolExtensionsType
            .GetMethod (
                nameof (GetSymbolType),
                new [] { typeof (ISymbol) })
            .CreateDelegate (typeof (GetSymbolTypeDelegate));

        public static ITypeSymbol GetSymbolType (this ISymbol symbol)
            => getSymbolType (symbol);

        #endregion

        #region GetEnclosingNamedTypeOrAssembly

        delegate ISymbol GetEnclosingNamedTypeOrAssemblyDelegate (
            SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken);

        static readonly GetEnclosingNamedTypeOrAssemblyDelegate getEnclosingNamedTypeOrAssembly =
            (GetEnclosingNamedTypeOrAssemblyDelegate)semanticModelExtensionsType
            .GetMethod (
                nameof (GetEnclosingNamedTypeOrAssembly),
                new [] {
                    typeof (SemanticModel),
                    typeof (int),
                    typeof (CancellationToken)
                })
            .CreateDelegate (typeof (GetEnclosingNamedTypeOrAssemblyDelegate));

        public static ISymbol GetEnclosingNamedTypeOrAssembly (
            this SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken)
            => getEnclosingNamedTypeOrAssembly (semanticModel, position, cancellationToken);

        #endregion
    }
}