//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

using Newtonsoft.Json;

using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    abstract class InteractiveObjectBase : IInteractiveObject
    {
        [JsonIgnore] readonly InteractiveItemPreparer itemPreparer;

        protected InteractiveItemPreparer ItemPreparer {
            get { return itemPreparer; }
        }

        public long Handle { get; set; }
        public long RepresentedObjectHandle { get; protected set; }
        public RepresentedType RepresentedType { get; protected set; }
        public int Depth { get; }

        [JsonConstructor]
        protected InteractiveObjectBase (
            long handle,
            long representedObjectHandle,
            RepresentedType representedType,
            int depth)
        {
            Handle = handle;
            RepresentedObjectHandle = representedObjectHandle;
            RepresentedType = representedType;
            Depth = depth;
        }

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
    }
}