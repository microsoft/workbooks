//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

#if IOS || MAC

using System.Runtime.InteropServices;

using ObjCRuntime;
using Foundation;

#endif

namespace Xamarin.Interactive
{
    static class NativeExceptionHandler
    {
        struct Disposable : IDisposable
        {
            public void Dispose () => Release ();
        }

        public class TrappedNativeException : Exception
        {
#if IOS || MAC
            public NSException NativeException { get; }

            public TrappedNativeException (NSException realException) : 
                base ($"A native exception was thrown: {realException.Description}") 
            {
                NativeException = realException;
            }
#endif
        }

#if IOS || MAC
        [DllImport (Constants.ObjectiveCLibrary)]
        static extern IntPtr objc_setExceptionPreprocessor (IntPtr handler);

        delegate IntPtr ObjCExceptionPreprocessorDelegate (IntPtr exceptionPtr);

        static readonly ObjCExceptionPreprocessorDelegate objCExceptionPreprocessor
            = new ObjCExceptionPreprocessorDelegate (ObjCExceptionPreprocessor);

        static readonly IntPtr objCExceptionPreprocessorFnptr
            = Marshal.GetFunctionPointerForDelegate (objCExceptionPreprocessor);

        static volatile IntPtr originalHandler;

        static IntPtr ObjCExceptionPreprocessor (IntPtr exceptionPtr)
        {
            throw new TrappedNativeException (Runtime.GetNSObject<NSException> (exceptionPtr));
        }

        public static IDisposable Trap ()
        {
            if (originalHandler == IntPtr.Zero)
                originalHandler = objc_setExceptionPreprocessor (objCExceptionPreprocessorFnptr);

            return new Disposable ();
        }

        public static void Release ()
        {
            if (originalHandler != IntPtr.Zero) {
                objc_setExceptionPreprocessor (originalHandler);
                originalHandler = IntPtr.Zero;
            }
        }
#else
        public static IDisposable Trap ()
            => new Disposable ();

        public static void Release ()
        {
        }
#endif
    }
}