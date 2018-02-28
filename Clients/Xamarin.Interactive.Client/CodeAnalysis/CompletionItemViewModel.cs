//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.CodeAnalysis.Completion;

namespace Xamarin.Interactive.CodeAnalysis
{
    sealed class CompletionItemViewModel
    {
        public string DisplayText { get; }

        public string InsertionText { get; }

        public string ItemDetail { get; }

        public CompletionItem CompletionItem { get; }

        public CompletionItemViewModel (CompletionItem completion)
        {
            CompletionItem = completion;

            DisplayText = completion.ToString ();

            if (completion.Properties.TryGetValue ("InsertionText", out var insertionText))
                InsertionText = insertionText;

            if (completion.Properties.TryGetValue (CompletionController.ItemDetailPropertyName, out var itemDetail))
                ItemDetail = itemDetail;
        }
    }
}
