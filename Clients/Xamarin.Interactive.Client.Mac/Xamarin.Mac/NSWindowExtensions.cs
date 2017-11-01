//
// NSWindowExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Runtime.InteropServices;

using AppKit;
using ObjCRuntime;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Mac
{
    static class NSWindowExtensions
    {
        const string TAG = nameof (NSWindowExtensions);

        [DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        static extern void void_objc_msgSend_bool (IntPtr receiver, IntPtr selector, bool arg1);

        [DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        static extern bool bool_objc_msgSend_IntPtr (IntPtr receiver, IntPtr selector, IntPtr arg1);

        static readonly IntPtr nsWindowClassPtr = Class.GetHandle (typeof (NSWindow));

        static readonly IntPtr setAllowsAutomaticWindowTabbingSel =
            Selector.GetHandle ("setAllowsAutomaticWindowTabbing:");

        static readonly IntPtr respondsToSelectorSel
            = Selector.GetHandle ("respondsToSelector:");

        public static void DisableAutomaticWindowTabbing ()
        {
            using (NativeExceptionHandler.Trap ()) {
                try {
                    if (bool_objc_msgSend_IntPtr (
                        nsWindowClassPtr,
                        respondsToSelectorSel,
                        setAllowsAutomaticWindowTabbingSel))
                        void_objc_msgSend_bool (
                            nsWindowClassPtr,
                            setAllowsAutomaticWindowTabbingSel,
                            false);
                } catch (Exception e) {
                    Log.Error (TAG, e);
                }
            }
        }
    }
}