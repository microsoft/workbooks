//
// MouseMonitor.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014 Xamarin Inc.

using System;

using CoreGraphics;
using CoreFoundation;

namespace Xamarin.Interactive.Client.Mac
{
    sealed class MouseMonitor : IDisposable
    {
        CFMachPort tapPort;
        CFRunLoopSource runLoopSource;
        CGEvent.CGEventTapCallback eventTapCallback;

        public event Action<CGPoint> MouseUp;
        public event Action<CGPoint> MouseMoved;

        public MouseMonitor ()
        {
            tapPort = CGEvent.CreateTap (
                CGEventTapLocation.Session,
                CGEventTapPlacement.TailAppend,
                CGEventTapOptions.ListenOnly,
                CGEventMask.LeftMouseUp | CGEventMask.MouseMoved,
                eventTapCallback = new CGEvent.CGEventTapCallback (HandleEventTap),
                IntPtr.Zero);

            runLoopSource = tapPort.CreateRunLoopSource ();
            CFRunLoop.Current.AddSource (runLoopSource, CFRunLoop.ModeDefault);
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        void Dispose (bool disposing)
        {
            if (runLoopSource != null) {
                CFRunLoop.Current.RemoveSource (runLoopSource, CFRunLoop.ModeDefault);
                runLoopSource.Dispose ();
                runLoopSource = null;
            }

            if (tapPort != null) {
                tapPort.Dispose ();
                tapPort = null;
            }

            if (eventTapCallback != null)
                eventTapCallback = null;
        }

        IntPtr HandleEventTap (IntPtr proxy, CGEventType eventType, IntPtr eventRef, IntPtr userInfo)
        {
            Action<CGPoint> handler = null;

            switch (eventType) {
            case CGEventType.LeftMouseUp:
            case CGEventType.RightMouseUp:
            case CGEventType.OtherMouseUp:
                handler = MouseUp;
                break;
            case CGEventType.MouseMoved:
                handler = MouseMoved;
                break;
            }

            if (handler != null)
                handler (new CGEvent (eventRef).Location);

            return IntPtr.Zero;
        }
    }
}