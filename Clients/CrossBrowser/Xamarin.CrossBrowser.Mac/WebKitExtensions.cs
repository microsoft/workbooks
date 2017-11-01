//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

using ObjCRuntime;
using JavaScriptCore;
using WebKit;

namespace Xamarin.CrossBrowser
{
    static class WebKitExtensions
    {
        static readonly IntPtr jsValueSelector = Selector.GetHandle ("JSValue");

        [DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        static extern IntPtr IntPtr_objc_msgSend (IntPtr receiver, IntPtr selector);

        public static JSValue GetJSValue (this WebScriptObject wso)
        {
            return Runtime.GetNSObject<JSValue> (IntPtr_objc_msgSend (wso.Handle, jsValueSelector));
        }
    }
}