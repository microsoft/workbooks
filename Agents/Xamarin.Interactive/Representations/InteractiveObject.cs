//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
    abstract class InteractiveObject : InteractiveObjectBase
    {
        [JsonObject]
        public sealed class GetMemberValueError
        {
            public ExceptionNode Exception { get; }

            [JsonConstructor]
            GetMemberValueError (ExceptionNode exception)
                => Exception = exception;

            public GetMemberValueError (Exception exception = null)
            {
                if (exception != null)
                    Exception = ExceptionNode.Create (exception);
            }
        }

        protected InteractiveObject (int depth, InteractiveItemPreparer itemPreparer)
            : base (depth, itemPreparer)
        {
        }

        [JsonConstructor]
        protected InteractiveObject (
            long handle,
            long representedObjectHandle,
            RepresentedType representedType,
            int depth,
            bool hasMembers,
            RepresentedMemberInfo [] members,
            object [] values,
            string toStringRepresentation,
            bool suppressToStringRepresentation)
            : base (
                handle,
                representedObjectHandle,
                representedType,
                depth)
        {
            HasMembers = hasMembers;
            Members = members;
            Values = values;
            ToStringRepresentation = toStringRepresentation;
            SuppressToStringRepresentation = suppressToStringRepresentation;
        }

        public bool HasMembers { get; protected set; }
        public RepresentedMemberInfo [] Members { get; protected set; }
        public object [] Values { get; protected set; }
        public string ToStringRepresentation { get; protected set; }
        public bool SuppressToStringRepresentation { get; protected set; }

        protected abstract void ReadMembers ();

        [JsonObject]
        public struct InteractMessage
        {
            public int MemberIndex;
            public int RepresentationIndex;
        }

        [JsonObject]
        public struct ReadAllMembersInteractMessage
        {
        }

        protected override IInteractiveObject Interact (bool isUserInteraction, object message)
        {
            if (message is ReadAllMembersInteractMessage ||
                (!isUserInteraction && message == null && Depth == 0)) {
                ReadMembers ();
                return this;
            }

            if (!isUserInteraction)
                return null;

            if (message == null)
                throw new ArgumentNullException (nameof(message));

            if (!(message is InteractMessage))
                throw new ArgumentException ($"must be a {typeof(InteractMessage)}", nameof (message));

            var interactMessage = (InteractMessage)message;
            var value = Values [interactMessage.MemberIndex];
            var interactiveValue = value as InteractiveObject;
            var representedObject = value as RepresentedObject;

            if (interactiveValue == null && representedObject != null)
                interactiveValue = representedObject [interactMessage.RepresentationIndex]
                    as InteractiveObject;

            if (interactiveValue == null)
                throw new ArgumentException ("message does not point to an InteractiveObject",
                    nameof (message));

            interactiveValue.ReadMembers ();
            return interactiveValue;
        }
    }
}