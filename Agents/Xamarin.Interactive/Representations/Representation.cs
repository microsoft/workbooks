//
// Representation.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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