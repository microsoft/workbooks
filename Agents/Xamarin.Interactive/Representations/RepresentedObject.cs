//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
    // This is intentionally a specialized collection that is optimized for
    // zero or two results, which will be the most common cases with zero
    // implying null and two being typical for a tuple of both a raw value
    // and an InteractiveObject representation. We don't return an array
    // directly since we might tack on more result metadata in the future.
    [Serializable]
    public sealed class RepresentedObject : IReadOnlyList<object>
    {
        readonly RepresentedType representedType;
        Representation [] representations;
        int count;

        public RepresentedType RepresentedType => representedType;

        public RepresentedObject (Type representedType)
            : this (RepresentedType.Lookup (representedType))
        {
        }

        public RepresentedObject (RepresentedType representedType)
        {
            this.representedType = representedType;
        }

        public void Add (object representation)
            => Add (representation is Representation
                ? (Representation)representation
                : new Representation (representation));

        public void Add (Representation representation)
        {
            if (representation.Value == null)
                return;

            if (representations == null)
                representations = new Representation [2];

            // if we find a representation with the same value, replace it
            // with the updated one, which may have augmented metadata
            for (int i = 0; i < count; i++) {
                if (Equals (representations [i].Value, representation.Value)) {
                    representations [i] = representation;
                    return;
                }
            }

            if (representations.Length == count)
                Array.Resize (ref representations, representations.Length << 1);

            representations [count++] = representation;
        }

        public Representation GetRepresentation (int index)
        {
            if (representations == null)
                throw new IndexOutOfRangeException ();

            return representations [index];
        }

        #region IReadOnlyList

        public int Count => count;

        public object this [int index] {
            get {
                if (representations == null)
                    throw new IndexOutOfRangeException ();
                return representations [index].Value;
            }
        }

        public IEnumerator<object> GetEnumerator ()
        {
            if (representations != null) {
                for (int i = 0; i < count; i++)
                    yield return representations [i].Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

        #endregion
    }
}