//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive
{
    static class MainThread
    {
        const string TAG = nameof (MainThread);

        static int mainThreadId;
        static bool initialized;

        public static void Initialize ()
        {
            if (initialized)
                return;

            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            SynchronizationContext = SynchronizationContext.Current;
            TaskScheduler = SynchronizationContext == null
                ? null
                : TaskScheduler.FromCurrentSynchronizationContext ();

            initialized = true;
        }

        public static void Reinitialize ()
        {
            initialized = false;
            Initialize ();
        }

        public static SynchronizationContext SynchronizationContext { get; private set; }
        public static TaskScheduler TaskScheduler { get; private set; }

        public static void Ensure ([CallerMemberName] string caller = null)
        {
            if (SynchronizationContext == null) {
                Log.Warning (
                    TAG,
                    $"MainThread.Ensure desired in {caller} but MainThread " +
                    "has no associated SynchronizationContext");
            } else if (Thread.CurrentThread.ManagedThreadId != mainThreadId) {
                Log.Error (TAG, $"MainThread.Ensure failed in {caller}");
                throw new Exception ($"{caller} must be invoked on main thread");
            }
        }

        public static void Post (Action handler)
        {
            if (SynchronizationContext == null || Thread.CurrentThread.ManagedThreadId == mainThreadId)
                handler ();
            else
                SynchronizationContext.Post (state => ((Action)state) (), handler);
        }

        public static void Send (Action handler)
        {
            if (SynchronizationContext == null || Thread.CurrentThread.ManagedThreadId == mainThreadId)
                handler ();
            else
                SynchronizationContext.Send (state => ((Action)state) (), handler);
        }

        public static void ContinueWithOnMainThread (
            this Task task,
            Action<Task> continuationAction,
            TaskContinuationOptions continuationOptions = TaskContinuationOptions.None,
            CancellationToken cancellationToken = default (CancellationToken))
            => task.ContinueWith (
                continuationAction,
                cancellationToken,
                continuationOptions,
                TaskScheduler ?? TaskScheduler.Current);

        public static Task SendAsync (
            Action<CancellationToken> handler,
            CancellationToken cancellationToken = default (CancellationToken))
            => SendAsync (ct => {
                handler (ct);
                return new TaskCompletionSource.Void ();
            }, cancellationToken);

        public static Task SendAsync (Action handler)
            => SendAsync (ct => {
                handler ();
                return new TaskCompletionSource.Void ();
            });

        public static Task<TResult> SendAsync<TResult> (
            Func<CancellationToken, TResult> handler,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            if (handler == null)
                throw new ArgumentNullException (nameof (handler));

            var taskCompletionSource = new TaskCompletionSource<TResult> ();

            if (cancellationToken.IsCancellationRequested) {
                taskCompletionSource.SetCanceled ();
                return taskCompletionSource.Task;
            }

            SynchronizationContext.Post (state => {
                try {
                    var result = handler (cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                        taskCompletionSource.SetCanceled ();
                    else
                        taskCompletionSource.SetResult (result);
                } catch (TaskCanceledException) {
                    taskCompletionSource.SetCanceled ();
                } catch (Exception e) {
                    taskCompletionSource.SetException (e);
                }
            }, null);

            return taskCompletionSource.Task;
        }
    }
}