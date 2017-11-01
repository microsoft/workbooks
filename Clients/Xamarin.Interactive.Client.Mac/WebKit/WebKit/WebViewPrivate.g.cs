//
// Auto-generated from generator.cs, do not edit
//
// We keep references to objects, so warning 414 is expected

#pragma warning disable 414

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using QTKit;
using Metal;
using OpenGL;
using AppKit;
using ModelIO;
using SceneKit;
using Security;
using CloudKit;
using AudioUnit;
using CoreVideo;
using CoreMedia;
using Foundation;
using ObjCRuntime;
using CoreGraphics;
using CoreLocation;
using AVFoundation;
using CoreAnimation;
using CoreFoundation;

namespace WebKit {
    public unsafe static partial class WebViewPrivate  {
        [CompilerGenerated]
        const string selInspector = "inspector";
        static readonly IntPtr selInspectorHandle = Selector.GetHandle ("inspector");
        
        [CompilerGenerated]
        static readonly IntPtr class_ptr = Class.GetHandle ("WebView");
        
        [Export ("inspector")]
        [CompilerGenerated]
        public static WebInspector GetInspector (this WebView This)
        {
            return  Runtime.GetNSObject<WebInspector> (global::WebKit.Messaging.IntPtr_objc_msgSend (This.Handle, selInspectorHandle));
        }
        
    } /* class WebViewPrivate */
}
