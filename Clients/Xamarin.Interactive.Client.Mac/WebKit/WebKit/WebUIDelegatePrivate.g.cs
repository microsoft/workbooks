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
    [Register("WebUIDelegatePrivate", false)]
    public unsafe partial class WebUIDelegatePrivate : WebUIDelegate {
        [CompilerGenerated]
        const string selWebView_AddMessageToConsole_WithSource_ = "webView:addMessageToConsole:withSource:";
        static readonly IntPtr selWebView_AddMessageToConsole_WithSource_Handle = Selector.GetHandle ("webView:addMessageToConsole:withSource:");
        
        [CompilerGenerated]
        static readonly IntPtr class_ptr = Class.GetHandle ("WebUIDelegatePrivate");
        
        public override IntPtr ClassHandle { get { return class_ptr; } }
        
        [CompilerGenerated]
        [EditorBrowsable (EditorBrowsableState.Advanced)]
        [Export ("init")]
        public WebUIDelegatePrivate () : base (NSObjectFlag.Empty)
        {
            IsDirectBinding = GetType ().Assembly == global::WebKit.Messaging.this_assembly;
            if (IsDirectBinding) {
                InitializeHandle (global::WebKit.Messaging.IntPtr_objc_msgSend (this.Handle, global::ObjCRuntime.Selector.GetHandle ("init")), "init");
            } else {
                InitializeHandle (global::WebKit.Messaging.IntPtr_objc_msgSendSuper (this.SuperHandle, global::ObjCRuntime.Selector.GetHandle ("init")), "init");
            }
        }

        [CompilerGenerated]
        [EditorBrowsable (EditorBrowsableState.Advanced)]
        protected WebUIDelegatePrivate (NSObjectFlag t) : base (t)
        {
            IsDirectBinding = GetType ().Assembly == global::WebKit.Messaging.this_assembly;
        }

        [CompilerGenerated]
        [EditorBrowsable (EditorBrowsableState.Advanced)]
        protected internal WebUIDelegatePrivate (IntPtr handle) : base (handle)
        {
            IsDirectBinding = GetType ().Assembly == global::WebKit.Messaging.this_assembly;
        }

        [Export ("webView:addMessageToConsole:withSource:")]
        [CompilerGenerated]
        public virtual void AddMessageToConsole (WebView webView, NSDictionary message, NSString source)
        {
            if (webView == null)
                throw new ArgumentNullException ("webView");
            if (message == null)
                throw new ArgumentNullException ("message");
            if (source == null)
                throw new ArgumentNullException ("source");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr_IntPtr_IntPtr (this.Handle, selWebView_AddMessageToConsole_WithSource_Handle, webView.Handle, message.Handle, source.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr_IntPtr_IntPtr (this.SuperHandle, selWebView_AddMessageToConsole_WithSource_Handle, webView.Handle, message.Handle, source.Handle);
            }
        }
        
    } /* class WebUIDelegatePrivate */
}
