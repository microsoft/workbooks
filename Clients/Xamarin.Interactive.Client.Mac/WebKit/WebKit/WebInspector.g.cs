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
    [Register("WebInspector", true)]
    public unsafe partial class WebInspector : NSObject {
        [CompilerGenerated]
        const string selAttach_ = "attach:";
        static readonly IntPtr selAttach_Handle = Selector.GetHandle ("attach:");
        [CompilerGenerated]
        const string selClose_ = "close:";
        static readonly IntPtr selClose_Handle = Selector.GetHandle ("close:");
        [CompilerGenerated]
        const string selDetach_ = "detach:";
        static readonly IntPtr selDetach_Handle = Selector.GetHandle ("detach:");
        [CompilerGenerated]
        const string selIsDebuggingJavaScript = "isDebuggingJavaScript";
        static readonly IntPtr selIsDebuggingJavaScriptHandle = Selector.GetHandle ("isDebuggingJavaScript");
        [CompilerGenerated]
        const string selIsJavaScriptProfilingEnabled = "isJavaScriptProfilingEnabled";
        static readonly IntPtr selIsJavaScriptProfilingEnabledHandle = Selector.GetHandle ("isJavaScriptProfilingEnabled");
        [CompilerGenerated]
        const string selIsOpen = "isOpen";
        static readonly IntPtr selIsOpenHandle = Selector.GetHandle ("isOpen");
        [CompilerGenerated]
        const string selIsProfilingJavaScript = "isProfilingJavaScript";
        static readonly IntPtr selIsProfilingJavaScriptHandle = Selector.GetHandle ("isProfilingJavaScript");
        [CompilerGenerated]
        const string selIsTimelineProfilingEnabled = "isTimelineProfilingEnabled";
        static readonly IntPtr selIsTimelineProfilingEnabledHandle = Selector.GetHandle ("isTimelineProfilingEnabled");
        [CompilerGenerated]
        const string selSetIsJavaScriptProfilingEnabled_ = "setIsJavaScriptProfilingEnabled:";
        static readonly IntPtr selSetIsJavaScriptProfilingEnabled_Handle = Selector.GetHandle ("setIsJavaScriptProfilingEnabled:");
        [CompilerGenerated]
        const string selSetIsTimelineProfilingEnabled_ = "setIsTimelineProfilingEnabled:";
        static readonly IntPtr selSetIsTimelineProfilingEnabled_Handle = Selector.GetHandle ("setIsTimelineProfilingEnabled:");
        [CompilerGenerated]
        const string selShow_ = "show:";
        static readonly IntPtr selShow_Handle = Selector.GetHandle ("show:");
        [CompilerGenerated]
        const string selShowConsole_ = "showConsole:";
        static readonly IntPtr selShowConsole_Handle = Selector.GetHandle ("showConsole:");
        [CompilerGenerated]
        const string selStartDebuggingJavaScript_ = "startDebuggingJavaScript:";
        static readonly IntPtr selStartDebuggingJavaScript_Handle = Selector.GetHandle ("startDebuggingJavaScript:");
        [CompilerGenerated]
        const string selStartProfilingJavaScript_ = "startProfilingJavaScript:";
        static readonly IntPtr selStartProfilingJavaScript_Handle = Selector.GetHandle ("startProfilingJavaScript:");
        [CompilerGenerated]
        const string selStopDebuggingJavaScript_ = "stopDebuggingJavaScript:";
        static readonly IntPtr selStopDebuggingJavaScript_Handle = Selector.GetHandle ("stopDebuggingJavaScript:");
        [CompilerGenerated]
        const string selStopProfilingJavaScript_ = "stopProfilingJavaScript:";
        static readonly IntPtr selStopProfilingJavaScript_Handle = Selector.GetHandle ("stopProfilingJavaScript:");
        [CompilerGenerated]
        const string selToggleDebuggingJavaScript_ = "toggleDebuggingJavaScript:";
        static readonly IntPtr selToggleDebuggingJavaScript_Handle = Selector.GetHandle ("toggleDebuggingJavaScript:");
        [CompilerGenerated]
        const string selToggleProfilingJavaScript_ = "toggleProfilingJavaScript:";
        static readonly IntPtr selToggleProfilingJavaScript_Handle = Selector.GetHandle ("toggleProfilingJavaScript:");
        
        [CompilerGenerated]
        static readonly IntPtr class_ptr = Class.GetHandle ("WebInspector");
        
        public override IntPtr ClassHandle { get { return class_ptr; } }
        
        [CompilerGenerated]
        [EditorBrowsable (EditorBrowsableState.Advanced)]
        protected WebInspector (NSObjectFlag t) : base (t)
        {
            IsDirectBinding = GetType ().Assembly == global::WebKit.Messaging.this_assembly;
        }

        [CompilerGenerated]
        [EditorBrowsable (EditorBrowsableState.Advanced)]
        protected internal WebInspector (IntPtr handle) : base (handle)
        {
            IsDirectBinding = GetType ().Assembly == global::WebKit.Messaging.this_assembly;
        }

        [Export ("attach:")]
        [CompilerGenerated]
        public virtual void Attach (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selAttach_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selAttach_Handle, sender.Handle);
            }
        }
        
        [Export ("close:")]
        [CompilerGenerated]
        public virtual void Close (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selClose_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selClose_Handle, sender.Handle);
            }
        }
        
        [Export ("detach:")]
        [CompilerGenerated]
        public virtual void Detach (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selDetach_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selDetach_Handle, sender.Handle);
            }
        }
        
        [Export ("show:")]
        [CompilerGenerated]
        public virtual void Show (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selShow_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selShow_Handle, sender.Handle);
            }
        }
        
        [Export ("showConsole:")]
        [CompilerGenerated]
        public virtual void ShowConsole (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selShowConsole_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selShowConsole_Handle, sender.Handle);
            }
        }
        
        [Export ("startDebuggingJavaScript:")]
        [CompilerGenerated]
        public virtual void StartDebuggingJavaScript (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selStartDebuggingJavaScript_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selStartDebuggingJavaScript_Handle, sender.Handle);
            }
        }
        
        [Export ("startProfilingJavaScript:")]
        [CompilerGenerated]
        public virtual void StartProfilingJavaScript (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selStartProfilingJavaScript_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selStartProfilingJavaScript_Handle, sender.Handle);
            }
        }
        
        [Export ("stopDebuggingJavaScript:")]
        [CompilerGenerated]
        public virtual void StopDebuggingJavaScript (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selStopDebuggingJavaScript_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selStopDebuggingJavaScript_Handle, sender.Handle);
            }
        }
        
        [Export ("stopProfilingJavaScript:")]
        [CompilerGenerated]
        public virtual void StopProfilingJavaScript (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selStopProfilingJavaScript_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selStopProfilingJavaScript_Handle, sender.Handle);
            }
        }
        
        [Export ("toggleDebuggingJavaScript:")]
        [CompilerGenerated]
        public virtual void ToggleDebuggingJavaScript (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selToggleDebuggingJavaScript_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selToggleDebuggingJavaScript_Handle, sender.Handle);
            }
        }
        
        [Export ("toggleProfilingJavaScript:")]
        [CompilerGenerated]
        public virtual void ToggleProfilingJavaScript (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException ("sender");
            if (IsDirectBinding) {
                global::WebKit.Messaging.void_objc_msgSend_IntPtr (this.Handle, selToggleProfilingJavaScript_Handle, sender.Handle);
            } else {
                global::WebKit.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, selToggleProfilingJavaScript_Handle, sender.Handle);
            }
        }
        
        [CompilerGenerated]
        public virtual bool IsDebuggingJavaScript {
            [Export ("isDebuggingJavaScript")]
            get {
                if (IsDirectBinding) {
                    return global::WebKit.Messaging.bool_objc_msgSend (this.Handle, selIsDebuggingJavaScriptHandle);
                } else {
                    return global::WebKit.Messaging.bool_objc_msgSendSuper (this.SuperHandle, selIsDebuggingJavaScriptHandle);
                }
            }
            
        }
        
        [CompilerGenerated]
        public virtual bool IsJavaScriptProfilingEnabled {
            [Export ("isJavaScriptProfilingEnabled")]
            get {
                if (IsDirectBinding) {
                    return global::WebKit.Messaging.bool_objc_msgSend (this.Handle, selIsJavaScriptProfilingEnabledHandle);
                } else {
                    return global::WebKit.Messaging.bool_objc_msgSendSuper (this.SuperHandle, selIsJavaScriptProfilingEnabledHandle);
                }
            }
            
            [Export ("setIsJavaScriptProfilingEnabled:")]
            set {
                if (IsDirectBinding) {
                    global::WebKit.Messaging.void_objc_msgSend_bool (this.Handle, selSetIsJavaScriptProfilingEnabled_Handle, value);
                } else {
                    global::WebKit.Messaging.void_objc_msgSendSuper_bool (this.SuperHandle, selSetIsJavaScriptProfilingEnabled_Handle, value);
                }
            }
        }
        
        [CompilerGenerated]
        public virtual bool IsOpen {
            [Export ("isOpen")]
            get {
                if (IsDirectBinding) {
                    return global::WebKit.Messaging.bool_objc_msgSend (this.Handle, selIsOpenHandle);
                } else {
                    return global::WebKit.Messaging.bool_objc_msgSendSuper (this.SuperHandle, selIsOpenHandle);
                }
            }
            
        }
        
        [CompilerGenerated]
        public virtual bool IsProfilingJavaScript {
            [Export ("isProfilingJavaScript")]
            get {
                if (IsDirectBinding) {
                    return global::WebKit.Messaging.bool_objc_msgSend (this.Handle, selIsProfilingJavaScriptHandle);
                } else {
                    return global::WebKit.Messaging.bool_objc_msgSendSuper (this.SuperHandle, selIsProfilingJavaScriptHandle);
                }
            }
            
        }
        
        [CompilerGenerated]
        public virtual bool IsTimelineProfilingEnabled {
            [Export ("isTimelineProfilingEnabled")]
            get {
                if (IsDirectBinding) {
                    return global::WebKit.Messaging.bool_objc_msgSend (this.Handle, selIsTimelineProfilingEnabledHandle);
                } else {
                    return global::WebKit.Messaging.bool_objc_msgSendSuper (this.SuperHandle, selIsTimelineProfilingEnabledHandle);
                }
            }
            
            [Export ("setIsTimelineProfilingEnabled:")]
            set {
                if (IsDirectBinding) {
                    global::WebKit.Messaging.void_objc_msgSend_bool (this.Handle, selSetIsTimelineProfilingEnabled_Handle, value);
                } else {
                    global::WebKit.Messaging.void_objc_msgSendSuper_bool (this.SuperHandle, selSetIsTimelineProfilingEnabled_Handle, value);
                }
            }
        }
        
    } /* class WebInspector */
}
