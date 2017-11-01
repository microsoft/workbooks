//
// InteractiveObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;

using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
	[Serializable]
	abstract class InteractiveObjectBase : IInteractiveObject
	{
		[NonSerialized] readonly InteractiveItemPreparer itemPreparer;

		protected InteractiveItemPreparer ItemPreparer {
			get { return itemPreparer; }
		}

		public long Handle { get; set; }
		public long RepresentedObjectHandle { get; protected set; }
		public RepresentedType RepresentedType { get; protected set; }
		public int Depth { get; private set; }

		protected InteractiveObjectBase (int depth, InteractiveItemPreparer itemPreparer)
		{
			if (itemPreparer == null)
				throw new ArgumentNullException (nameof(itemPreparer));

			Depth = depth;

			this.itemPreparer = itemPreparer;
		}

		public virtual void Initialize ()
		{
			Prepare ();
		}

		public IInteractiveObject Interact (object message)
		{
			return Interact (true, message);
		}

		public virtual void Reset ()
		{
			Prepare ();
		}

		protected abstract void Prepare ();
		protected abstract IInteractiveObject Interact (bool isUserInteraction, object message);

		#region ISerializable Helpers

		// InteractiveObjectBase does not implement ISerializable as to not force subclasses into it,
		// but it does provide facilities to easily help subclasses should they choose to do so.
		// Always ensure InteractiveObjectBase serializes the same data via ISerializable or reflection!

		protected InteractiveObjectBase (SerializationInfo info, StreamingContext context)
		{
			Handle = info.GetInt64 (nameof(Handle));
			RepresentedType = info.GetValue<RepresentedType> (nameof(RepresentedType));
			Depth = info.GetInt32 (nameof(Depth));
		}

		protected virtual void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			info.AddValue (nameof(Handle), Handle);
			info.AddValue (nameof(RepresentedType), RepresentedType);
			info.AddValue (nameof(Depth), Depth);
		}

		#endregion

		void ISerializableObject.Serialize (ObjectSerializer serializer)
		{
			throw new NotImplementedException ();
		}
	}
}