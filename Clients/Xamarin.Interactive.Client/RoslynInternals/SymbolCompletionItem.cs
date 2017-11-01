//
// SymbolCompletionItem.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace Xamarin.Interactive.RoslynInternals
{
	static class SymbolCompletionItem
	{
		static readonly Type symbolCompletionItemType = typeof (CompletionItem).Assembly.GetType (
			"Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem");

		static readonly MethodInfo getSymbolsAsync = symbolCompletionItemType.GetMethod ("GetSymbolsAsync");

		static PropertyInfo taskOfImmutableSymbolArrayTypeResultProperty;

		public static Task<ImmutableArray<ISymbol>> GetSymbolsAsync (CompletionItem item, Document document, CancellationToken cancellationToken)
		{
			return ((Task)getSymbolsAsync.Invoke (null, new object [] {
				item,
				document,
				cancellationToken
			})).ContinueWith (task => {
				if (taskOfImmutableSymbolArrayTypeResultProperty == null)
					taskOfImmutableSymbolArrayTypeResultProperty = task
						.GetType ()
						.GetProperty ("Result");

				return (ImmutableArray<ISymbol>)
					taskOfImmutableSymbolArrayTypeResultProperty.GetValue (task);
			});
		}
	}
}
