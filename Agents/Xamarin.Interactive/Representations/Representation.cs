//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    public struct Representation : IRepresentationObject
    {
        public static readonly Representation Empty = new Representation ();

        public object Value { get; }
        public bool CanEdit { get; }

        public Representation (object value, bool canEdit = false)
        {
            Value = value;
            CanEdit = canEdit;
        }

        internal Representation With (object value, bool canEdit)
            => new Representation (value, canEdit);

        void ISerializableObject.Serialize (ObjectSerializer serializer)
        {
            throw new NotImplementedException ();
        }
    }
}