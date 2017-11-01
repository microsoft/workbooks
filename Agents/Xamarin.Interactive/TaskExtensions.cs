//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive
{
    static class TaskExtensions
    {
        /// <summary>
        /// Signify that a task is "fire and forget", and that it is
        /// intentionally not awaited. Exceptions will be logged.
        /// </summary>
        public static void Forget (this Task task, string exceptionTag = null, bool log = true)
        {
            if (!log || task == null)
                return;

            task.ContinueWith (
                t => Log.Error (
                    exceptionTag ?? "!FAF",
                    "Exception dropped on floor",
                    task.Exception),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}