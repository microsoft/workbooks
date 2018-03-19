//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace Xamarin.Interactive.RoslynInternals
{
    sealed class CompletionHelper
    {
        static readonly Type completionHelperType = typeof (CompletionItem).Assembly.GetType (
            "Microsoft.CodeAnalysis.Completion.CompletionHelper");

        static readonly MethodInfo getHelper = completionHelperType.GetMethod (
            "GetHelper",
            new [] { typeof (Document) });
        static readonly MethodInfo matchesPattern = completionHelperType.GetMethod ("MatchesPattern");
        static readonly MethodInfo compareItems = completionHelperType.GetMethod ("CompareItems");

        readonly object internalInstance;

        CompletionHelper (object internalInstance)
        {
            this.internalInstance = internalInstance;
        }

        public static CompletionHelper GetHelper (Document document)
        {
            return new CompletionHelper (getHelper.Invoke (null, new object [] { document }));
        }

        public bool MatchesPattern (string text, string pattern, CultureInfo culture)
        {
            return (bool)matchesPattern.Invoke (internalInstance, new object [] {
                text,
                pattern,
                culture
            });
        }

        public int CompareItems (CompletionItem item1, CompletionItem item2, string filterText, CultureInfo culture)
        {
            return (int)compareItems.Invoke (internalInstance, new object [] {
                item1,
                item2,
                filterText,
                culture
            });
        }
    }
}
