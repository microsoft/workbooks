//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

using ObjCRuntime;
using Foundation;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Unified
{
    sealed class UnifiedNativeHelper : NativeHelper
    {
        public override IDisposable TrapNativeExceptions () => NativeExceptionHandler.Trap ();

        public override string CheckProperty (
            PropertyInfo property,
            object target,
            RepresentedType declaringType)
        {
            if (!typeof (NSObject).IsAssignableFrom (declaringType.ResolvedType))
                return null;

            var selName = property
                ?.GetGetMethod (true)
                ?.GetCustomAttribute<ExportAttribute> ()
                ?.Selector;
            if (selName == null)
                return null;

            var objCSelector = new Selector (selName);
            if (objCSelector == null || objCSelector.Handle == IntPtr.Zero)
                return null;

            var nso = target as NSObject;
            if (nso == null || nso.RespondsToSelector (objCSelector))
                return null;

            return string.Format ("{0} instance 0x{1:x} does not respond to selector {2}",
                target.GetType (), nso.Handle, objCSelector.Name);
        }
    }
}