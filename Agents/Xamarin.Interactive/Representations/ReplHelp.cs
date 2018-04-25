//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    sealed class ReplHelp : IRepresentationObject, IEnumerable<ReplHelp.Item>
    {
        [JsonObject]
        public struct Item : IRepresentationObject
        {
            public ITypeMember Member { get; }
            public string Description { get; }
            public bool ShowReturnType { get; }

            [JsonConstructor]
            public Item (ITypeMember member, string description, bool showReturnType = false)
            {
                Member = member;
                Description = description;
                ShowReturnType = showReturnType;
            }
        }

        readonly List<Item> items;
        public IReadOnlyList<Item> Items => items;

        [JsonConstructor]
        public ReplHelp (IReadOnlyList<Item> items = null)
            => this.items = new List<Item> (items ?? Array.Empty<Item> ());

        public void Add (Item item)
            => items.Add (item);

        public IEnumerator<Item> GetEnumerator ()
            => items.GetEnumerator ();

        IEnumerator IEnumerable.GetEnumerator ()
            => GetEnumerator ();
    }
}