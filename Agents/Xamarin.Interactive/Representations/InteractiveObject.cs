//
// InteractiveObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
	[Serializable]
	abstract class InteractiveObject : InteractiveObjectBase
	{
		[Serializable]
		public sealed class GetMemberValueError : IRepresentationObject
		{
			public ExceptionNode Exception { get; private set; }

			public GetMemberValueError (Exception exception = null)
			{
				if (exception != null)
					Exception = ExceptionNode.Create (exception);
			}

			void ISerializableObject.Serialize (ObjectSerializer serializer)
			{
				throw new NotImplementedException ();
			}
		}

		protected InteractiveObject (int depth, InteractiveItemPreparer itemPreparer)
			: base (depth, itemPreparer)
		{
		}

		public bool HasMembers { get; protected set; }
		public RepresentedMemberInfo [] Members { get; protected set; }
		public object [] Values { get; protected set; }
		public string ToStringRepresentation { get; protected set; }
		public bool SuppressToStringRepresentation { get; protected set; }

		protected abstract void ReadMembers ();

		[Serializable]
		public struct InteractMessage
		{
			public int MemberIndex;
			public int RepresentationIndex;
		}

		[Serializable]
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