//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis.Completion;

namespace Xamarin.Interactive.Client.Monaco
{
    enum MonacoCompletionItemKind
    {
        Text = 0,
        Method = 1,
        Function = 2,
        Constructor = 3,
        Field = 4,
        Variable = 5,
        Class = 6,
        Interface = 7,
        Module = 8,
        Property = 9,
        Unit = 10,
        Value = 11,
        Enum = 12,
        Keyword = 13,
        Snippet = 14,
        Color = 15,
        File = 16,
        Reference = 17,
    }

    // TODO: Separate file? Do we need more extensions in XIC?
    public static class MonacoExtensions
    {
        public static int ToMonacoCompletionItemKind (ImmutableArray<string> completionTags)
        {
            const MonacoCompletionItemKind defaultKind = MonacoCompletionItemKind.Text;

            if (completionTags.Length == 0)
                return (int)defaultKind;

            if (completionTags.Contains (CompletionTags.Assembly)
                || completionTags.Contains (CompletionTags.Namespace)
                || completionTags.Contains (CompletionTags.Project)
                || completionTags.Contains (CompletionTags.Module))
                return (int)MonacoCompletionItemKind.Module;

            if (completionTags.Contains (CompletionTags.Class)
                || completionTags.Contains (CompletionTags.Structure))
                return (int)MonacoCompletionItemKind.Class;

            if (completionTags.Contains (CompletionTags.Constant)
                || completionTags.Contains (CompletionTags.Field)
                || completionTags.Contains (CompletionTags.Delegate)
                || completionTags.Contains (CompletionTags.Event)
                || completionTags.Contains (CompletionTags.Local))
                return (int)MonacoCompletionItemKind.Field;

            if (completionTags.Contains (CompletionTags.Enum)
                || completionTags.Contains (CompletionTags.EnumMember))
                return (int)MonacoCompletionItemKind.Enum;

            if (completionTags.Contains (CompletionTags.Method)
                || completionTags.Contains (CompletionTags.Operator))
                return (int)MonacoCompletionItemKind.Method;

            if (completionTags.Contains (CompletionTags.ExtensionMethod))
                return (int)MonacoCompletionItemKind.Function;

            if (completionTags.Contains (CompletionTags.Interface))
                return (int)MonacoCompletionItemKind.Interface;

            if (completionTags.Contains (CompletionTags.Property))
                return (int)MonacoCompletionItemKind.Property;

            if (completionTags.Contains (CompletionTags.Keyword))
                return (int)MonacoCompletionItemKind.Keyword;

            if (completionTags.Contains (CompletionTags.Reference))
                return (int)MonacoCompletionItemKind.Reference;

            if (completionTags.Contains (CompletionTags.Snippet))
                return (int)MonacoCompletionItemKind.Snippet;

            return (int)defaultKind;
        }
    }
}
