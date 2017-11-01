//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
    [Serializable]
    sealed class InteractiveEnumerable : InteractiveObjectBase
    {
        const string TAG = nameof (InteractiveEnumerable);
        const int defaultSliceSize = 10;

        [NonSerialized] IEnumerable enumerable;
        [NonSerialized] IEnumerator enumerator;

        public int Count { get; private set; }

        object [] slice;
        public object [] Slice {
            get { return slice; }
        }

        public bool IsLastSlice { get; private set; }

        public InteractiveEnumerable (int depth, IEnumerable enumerable,
            InteractiveItemPreparer itemPreparer) : base (depth, itemPreparer)
        {
            if (enumerable == null)
                throw new ArgumentNullException (nameof(enumerable));

            this.enumerable = enumerable;
            RepresentedType = RepresentedType.Lookup (enumerable.GetType ());
        }

        public override void Initialize ()
        {
            base.Initialize ();
            Interact (false, null);
        }

        public override void Reset ()
        {
            (enumerator as IDisposable)?.Dispose ();
            base.Reset ();
        }

        protected override void Prepare ()
        {
            enumerator = enumerable.GetEnumerator ();

            Count = -1;
            IsLastSlice = false;

            var array = enumerable as Array;
            if (array != null)
                Count = array.Length;

            var collection = enumerable as ICollection;
            if (collection != null)
                Count = collection.Count;
        }

        protected override IInteractiveObject Interact (bool isUserInteraction, object message)
        {
            if (!isUserInteraction && Depth > 0)
                return this;

            if (slice == null)
                slice = new object [defaultSliceSize];

            int i = 0;

            while (i < slice.Length) {
                // In case of error, default to true so we can show "cannot evaluate"
                var enumerated = true;
                object current = null;

                try {
                    enumerated = enumerator.MoveNext ();
                    if (enumerated)
                        current = enumerator.Current;
                } catch (Exception e) {
                    Log.Error (TAG, e);
                    current = new InteractiveObject.GetMemberValueError (e);
                }

                if (!enumerated) {
                    IsLastSlice = true;
                    break;
                }

                var result = new RepresentedObject (current?.GetType ());
                ItemPreparer (result, Depth + 1, current);
                if (result.Count == 0)
                    result = null;

                slice [i++] = result;
            }

            if (i < slice.Length)
                Array.Resize (ref slice, i);

            return this;
        }
    }
}