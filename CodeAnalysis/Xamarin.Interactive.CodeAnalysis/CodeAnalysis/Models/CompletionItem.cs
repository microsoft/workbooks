// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    [MonacoSerializable ("monaco.languages.CompletionItem")]
    public struct CompletionItem
    {
        public CompletionItemKind Kind { get; }

        public string Label { get; }

        [JsonProperty (NullValueHandling = NullValueHandling.Ignore)]
        public string InsertText { get; }

        [JsonProperty (NullValueHandling = NullValueHandling.Ignore)]
        public string Detail { get; }

        [JsonConstructor]
        public CompletionItem (
            CompletionItemKind kind,
            string label,
            string insertText = null,
            string detail = null)
        {
            Kind = kind;
            Label = label ?? throw new ArgumentNullException (nameof (label));
            InsertText = insertText;
            Detail = detail;
        }
    }
}