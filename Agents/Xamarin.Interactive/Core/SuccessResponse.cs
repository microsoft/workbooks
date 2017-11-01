//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class SuccessResponse : IObjectReference
    {
        static readonly SuccessResponse singleton = new SuccessResponse ();

        public static readonly Task<SuccessResponse> Task
            = System.Threading.Tasks.Task.FromResult (singleton);

        SuccessResponse ()
        {
        }

        object IObjectReference.GetRealObject (StreamingContext context) => singleton;
    }
}