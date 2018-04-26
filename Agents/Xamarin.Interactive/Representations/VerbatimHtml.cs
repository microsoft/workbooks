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