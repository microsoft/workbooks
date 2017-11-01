//
// Dispatch.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Xamarin.Interactive
{
    static class Dispatch
    {
        [DllImport ("libc")]
        static extern IntPtr dlsym (IntPtr handle, string symbol);

        [DllImport ("libc")]
        static extern IntPtr dlopen (string path, int mode);

        [DllImport ("libc")]
        static extern IntPtr dispatch_source_create (IntPtr type, UIntPtr handle, UIntPtr mask, IntPtr queue);

        public delegate void DispatchEventHandler (IntPtr userdata);

        [DllImport ("libc")]
        static extern void dispatch_source_set_event_handler_f (IntPtr source, DispatchEventHandler handler);

        [DllImport ("libc")]
        static extern void dispatch_source_set_timer (IntPtr source, ulong start, ulong interval, ulong leeway);

        [DllImport ("libc")]
        static extern void dispatch_resume (IntPtr obj);

        [DllImport ("libc")]
        static extern void dispatch_source_cancel (IntPtr source);

        [DllImport ("libc")]
        static extern void dispatch_main ();

        static IntPtr mainQueue;
        static IntPtr timerSource;

        static Dispatch ()
        {
            var libSystem = dlopen ("/usr/lib/libSystem.dylib", 0);
            timerSource = dlsym (libSystem, "_dispatch_source_type_timer");
            mainQueue = dlsym (libSystem, "_dispatch_main_q");
        }

        static Dictionary<IntPtr, DispatchEventHandler> callbacks = new Dictionary<IntPtr, DispatchEventHandler> ();

        public static IntPtr ScheduleRepeatingTimer (TimeSpan interval, DispatchEventHandler handler)
        {
            return ScheduleRepeatingTimer (interval, interval, handler);
        }

        public static IntPtr ScheduleRepeatingTimer (TimeSpan interval, TimeSpan leeway, DispatchEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException (nameof (handler));

            var timer = dispatch_source_create (timerSource, UIntPtr.Zero, UIntPtr.Zero, mainQueue);
            callbacks.Add (timer, handler);
            dispatch_source_set_event_handler_f (timer, handler);
            dispatch_source_set_timer (timer, 0, (ulong)interval.Ticks * 100, (ulong)leeway.Ticks * 100);
            dispatch_resume (timer);
            return timer;
        }

        public static void Cancel (IntPtr source)
        {
            dispatch_source_cancel (source);
            callbacks.Remove (source);
        }

        public static void RunMainLoop ()
        {
            dispatch_main ();
        }
    }
}