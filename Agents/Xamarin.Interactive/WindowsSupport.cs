//
// WindowsSupport.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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
