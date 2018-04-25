// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn.Internals
{
    sealed class CompletionHelper
    {
        static readonly Type completionHelperType = typeof (CompletionItem).Assembly.GetType (
            "Microsoft.CodeAnalysis.Completion.CompletionHelper");

        static readonly Func<Document, object> getHelper = (Func<Document, object>)completionHelperType
            .GetMethod (nameof (GetHelper), new [] { typeof (Document) })
            .CreateDelegate (typeof (Func<Document, object>));

        public static CompletionHelper GetHelper (Document document)
            => new CompletionHelper (getHelper (document));

        public delegate bool MatchesPatternDelegate (
            string text,
            string pattern,
            CultureInfo culture);

        public readonly MatchesPatternDelegate MatchesPattern;

        public delegate int CompareItemsDelegate (
            CompletionItem item1,
            CompletionItem item2,
            string filterText,
            CultureInfo culture);

        public readonly CompareItemsDelegate CompareItems;

        CompletionHelper (object internalInstance)
        {
            MatchesPattern = (MatchesPatternDelegate)completionHelperType
                .GetMethod (nameof (MatchesPattern))
                .CreateDelegate (typeof (MatchesPatternDelegate), internalInstance);

            CompareItems = (CompareItemsDelegate)completionHelperType
                .GetMethod (nameof (CompareItems))
                .CreateDelegate (typeof (CompareItemsDelegate), internalInstance);
        }
    }
}