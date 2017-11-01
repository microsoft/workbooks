//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Xamarin.Interactive
{
    static class WindowsSupport
    {
        [DllImport ("kernel32", EntryPoint = "LoadLibrary", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr NativeLoadLibrary ([MarshalAs (UnmanagedType.LPStr)] string lpFileName);

        public static IntPtr LoadLibrary (string libraryPath)
        {
            var handle = NativeLoadLibrary (libraryPath);

            if (handle == null)
                throw new Win32Exception (Marshal.GetLastWin32Error ());

            return handle;
        }
    }
}
