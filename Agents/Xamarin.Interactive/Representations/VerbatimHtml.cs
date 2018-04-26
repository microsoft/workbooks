//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    public sealed class VerbatimHtml
    {
        // Intentionally a private field so we do not serialize the content.
        // We will always send the value as part of ToStringRepresentation.
        // See VerbatimHtmlRenderer.tsx for details.
        readonly string content;

        public VerbatimHtml (StringBuilder builder)
            => content = builder?.ToString ();

        [JsonConstructor]
        public VerbatimHtml (string content)
            => this.content = content;

        public override string ToString ()
            => content;
    }
}