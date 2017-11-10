//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive
{
    class NativeHelper
    {
        struct Disposable : IDisposable
        {
            void IDisposable.Dispose () { }
        }

        readonly static IDisposable disposable = new Disposable ();

        static NativeHelper sharedInstance;
        public static NativeHelper SharedInstance {
            get {
                if (sharedInstance == null)
                    sharedInstance = new NativeHelper ();
                return sharedInstance;
            }
        }

        /// <summary>
        /// Replace default SharedInstance with this instance.
        /// </summary>
        public void Initialize ()
        {
            sharedInstance = this;
        }

        public virtual IDisposable TrapNativeExceptions ()
            => disposable;

        /// <summary>
        /// Check that a CLR property can be safely invoked on a target object that
        /// may wrap a native object.
        /// </summary>
        /// <returns>Null if invocation is safe, an error string suitable for an
        /// exception message otherwise.</returns>
        public virtual string CheckProperty (
            PropertyInfo property,
            object target,
            RepresentedType declaringType)
            => null;
    }
}