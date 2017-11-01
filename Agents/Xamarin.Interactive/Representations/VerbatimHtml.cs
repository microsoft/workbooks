//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    public sealed class VerbatimHtml : IRepresentationObject
    {
        readonly string content;

        public VerbatimHtml (StringBuilder builder)
        {
            content = builder?.ToString ();
        }

        public VerbatimHtml (string content)
        {
            this.content = content;
        }

        public override string ToString ()
        {
            return content;
        }

        void ISerializableObject.Serialize (ObjectSerializer serializer)
        {
            throw new NotImplementedException ();
        }
    }
}