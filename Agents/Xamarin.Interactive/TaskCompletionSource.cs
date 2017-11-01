//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Xamarin.Interactive
{
    sealed class TaskCompletionSource : TaskCompletionSource<TaskCompletionSource.Void>
    {
        internal struct Void
        {
        }

        public void SetResult () => SetResult (new Void ());
    }
}