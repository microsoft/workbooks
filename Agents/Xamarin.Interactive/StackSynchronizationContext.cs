//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Xamarin.Interactive
{
    class StackSynchronizationContext : SynchronizationContext
    {
        readonly ReaderWriterLockSlim contextsLock = new ReaderWriterLockSlim ();
        readonly Stack<SynchronizationContext> contexts = new Stack<SynchronizationContext> ();

        public StackSynchronizationContext (bool withCurrentSynchronizationContext = true)
        {
            if (withCurrentSynchronizationContext) {
                if (Current == null)
                    throw new ArgumentException (
                        "Current SynchronizationContext must be explicitly set when withCurrentSynchronizationContext=true");
                contexts.Push (Current);
            }
        }

        public SynchronizationContext PushContext (SynchronizationContext context)
        {
            if (context == null)
                throw new ArgumentNullException (nameof (context));

            contextsLock.EnterWriteLock ();
            try {
                contexts.Push (context);
                return context;
            } finally {
                contextsLock.ExitWriteLock ();
                OnContextChanged ();
            }
        }

        public SynchronizationContext PeekContext ()
        {
            contextsLock.EnterReadLock ();
            try {
                return contexts.Peek ();
            } finally {
                contextsLock.ExitReadLock ();
            }
        }

        public SynchronizationContext PopContext ()
        {
            contextsLock.EnterWriteLock ();
            try {
                var currentContext = contexts.Peek ();
                if (CanPopContext (currentContext, contexts.Count))
                    currentContext = contexts.Pop ();
                return currentContext;
            } finally {
                contextsLock.ExitWriteLock ();
                OnContextChanged ();
            }
        }

        public int ContextCount {
            get {
                contextsLock.EnterReadLock ();
                try {
                    return contexts.Count;
                } finally {
                    contextsLock.ExitReadLock ();
                }
            }
        }

        protected virtual bool CanPopContext (SynchronizationContext currentContext, int totalContexts)
            => true;

        protected virtual void OnContextChanged ()
        {
        }

        public sealed override SynchronizationContext CreateCopy ()
            => PeekContext ().CreateCopy ();

        public sealed override void Post (SendOrPostCallback d, object state)
            => PeekContext ().Post (d, state);

        public sealed override void Send (SendOrPostCallback d, object state)
            => PeekContext ().Send (d, state);

        public sealed override void OperationStarted ()
            => PeekContext ().OperationStarted ();

        public sealed override void OperationCompleted ()
            => PeekContext ().OperationCompleted ();

        public sealed override int Wait (IntPtr [] waitHandles, bool waitAll, int millisecondsTimeout)
            => PeekContext ().Wait (waitHandles, waitAll, millisecondsTimeout);
    }
}