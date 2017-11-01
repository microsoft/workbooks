//
// NSDocumentControllerExtensions.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Runtime.InteropServices;

using ObjCRuntime;

namespace AppKit
{
	static class NSDocumentControllerExtensions
	{
		[DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSendSuper")]
		static extern nint nint_objc_msgSendSuper (IntPtr receiver, IntPtr selector);

		static readonly Selector maximumRecentDocumentCount = new Selector ("maximumRecentDocumentCount");

		public static nint GetSuperMaximumRecentDocumentCount (this NSDocumentController documentController)
			=> nint_objc_msgSendSuper (
				documentController.SuperHandle,
				maximumRecentDocumentCount.Handle);
	}
}
