//
// Authors:
//   Stephen Toub <stoub@microsoft.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Xamarin.Interactive
{
    // Simplified from Stephen Toub's AsyncPump
    // https://blogs.msdn.microsoft.com/pfxteam/2012/01/20/await-synchronizationcontext-and-console-apps/
    class SingleThreadSynchronizationContext : SynchronizationContext
    {
        readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> queue =
            new BlockingCollection<KeyValuePair<SendOrPostCallback, object>> ();

        public override void Post (SendOrPostCallback d, object state)
        {
            queue.Add (new KeyValuePair<SendOrPostCallback, object> (d, state));
        }

        public override void Send (SendOrPostCallback d, object state)
        {
            throw new NotImplementedException ();
        }

        public void RunOnCurrentThread ()
        {
            foreach (var workItem in queue.GetConsumingEnumerable ())
                workItem.Key (workItem.Value);
        }

        public void Complete () => queue.CompleteAdding ();

        public static void StartRunLoopAsBackgroundThread (
            string threadName = nameof (SingleThreadSynchronizationContext) +
                "::" + nameof (StartRunLoopAsBackgroundThread))
        {
            var syncContext = new SingleThreadSynchronizationContext ();
            SetSynchronizationContext (syncContext);

            using (var threadStartWait = new ManualResetEvent (false)) {
                void Thread (object state)
                {
                    threadStartWait.Set ();
                    syncContext.RunOnCurrentThread ();
                }

                new Thread (Thread) {
                    IsBackground = true,
                    Name = threadName
                }.Start ();

                threadStartWait.WaitOne ();
            }
        }
    }
}