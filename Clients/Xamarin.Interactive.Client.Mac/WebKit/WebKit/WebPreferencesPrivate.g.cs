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
    public unsafe static partial class WebPreferencesPrivate  {
        [CompilerGenerated]
        const string selDeveloperExtrasEnabled = "developerExtrasEnabled";
        static readonly IntPtr selDeveloperExtrasEnabledHandle = Selector.GetHandle ("developerExtrasEnabled");
        [CompilerGenerated]
        const string selSetDeveloperExtrasEnabled_ = "setDeveloperExtrasEnabled:";
        static readonly IntPtr selSetDeveloperExtrasEnabled_Handle = Selector.GetHandle ("setDeveloperExtrasEnabled:");
        
        [CompilerGenerated]
        static readonly IntPtr class_ptr = Class.GetHandle ("WebPreferences");
        
        [Export ("developerExtrasEnabled")]
        [CompilerGenerated]
        public static bool GetDeveloperExtrasEnabled (this WebPreferences This)
        {
            return global::WebKit.Messaging.bool_objc_msgSend (This.Handle, selDeveloperExtrasEnabledHandle);
        }
        
        [Export ("setDeveloperExtrasEnabled:")]
        [CompilerGenerated]
        public static void SetDeveloperExtrasEnabled (this WebPreferences This, bool enabled)
        {
            global::WebKit.Messaging.void_objc_msgSend_bool (This.Handle, selSetDeveloperExtrasEnabled_Handle, enabled);
        }
        
    } /* class WebPreferencesPrivate */
}
