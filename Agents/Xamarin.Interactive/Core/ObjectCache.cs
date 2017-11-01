//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

namespace Xamarin.Interactive.Core
{
    /// <summary>
    /// A simple mapping unique IDs to object references and back.
    /// </summary>
    class ObjectCache
    {
        public static readonly ObjectCache Shared = new ObjectCache ();

        readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim ();
        readonly Dictionary<object, long> objectsToHandles = new Dictionary<object, long> ();
        readonly Dictionary<long, object> handlesToObjects = new Dictionary<long, object> ();

        long lastHandle;

        /// <summary>
        /// Get the handle for a given object, caching it if not already cached.
        /// </summary>
        public long GetHandle (object obj)
        {
            rwlock.EnterUpgradeableReadLock ();
            try {
                long handle;
                if (!objectsToHandles.TryGetValue (obj, out handle)) {
                    rwlock.EnterWriteLock ();
                    try {
                        handle = ++lastHandle;
                        objectsToHandles.Add (obj, handle);
                        handlesToObjects.Add (handle, obj);
                    } finally {
                        rwlock.ExitWriteLock ();
                    }
                }

                return handle;
            } finally {
                rwlock.ExitUpgradeableReadLock ();
            }
        }

        /// <summary>
        /// Get the object for a given handle or null.
        /// </summary>
        public object GetObject (long handle)
        {
            rwlock.EnterReadLock ();
            try {
                object obj;
                handlesToObjects.TryGetValue (handle, out obj);
                return obj;
            } finally {
                rwlock.ExitReadLock ();
            }
        }

        /// <summary>
        /// Clear all objects from the cache and reset the next cached handle to 0.
        /// </summary>
        public void ClearHandles ()
        {
            rwlock.EnterWriteLock ();
            try {
                lastHandle = 0;
                objectsToHandles.Clear ();
                handlesToObjects.Clear ();
            } finally {
                rwlock.ExitWriteLock ();
            }
        }
    }
}