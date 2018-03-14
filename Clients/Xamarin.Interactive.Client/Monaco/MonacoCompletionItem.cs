//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis.Completion;

namespace Xamarin.Interactive.Client.Monaco
{
    class MonacoCompletionItem
    {
        public string Label { get; }

        [JsonProperty (NullValueHandling = NullValueHandling.Ignore)]
        public string InsertText { get; }

        [JsonProperty (NullValueHandling = NullValueHandling.Ignore)]
        public string Detail { get; }

        // Corresponds to Monaco's CompletionItemKind enum
        // TODO: Can we type this as enum or no?
        public int Kind { get; }

        public MonacoCompletionItem (CompletionItemViewModel itemViewModel)
        {
            Label = itemViewModel.DisplayText;
            // TODO: Can we tell the serializer to exclude insertText and detail if null? Right now I'm having to
            //       loop through the list on the client side and replace null with undefined (Monaco breaks otherwise).
            InsertText = itemViewModel.InsertionText;
            Detail = itemViewModel.ItemDetail;
            Kind = MonacoExtensions.ToMonacoCompletionItemKind (itemViewModel.CompletionItem.Tags);
        }
    }
}