using System;
using System.Runtime.InteropServices;

using Darwin;

namespace MonoTouch.Hosting
{

    [StructLayout (LayoutKind.Sequential, Pack = 0)]
    struct KernelEvent
    {
        public IntPtr Ident;
        public EventFilter Filter;
        public EventFlags Flags;
        public uint FilterFlags;
        public IntPtr Data;
        public IntPtr UserData;
    }

    static class KQueue
    {

        // FIXME: It seems the monomac bindings for kqueue/kevent are broken.
        //  We'll use our own definitions here until monomac is fixed...

        public static TimeSpec ToTimespec (this TimeSpan ts)
        {
            return new TimeSpec {
                Seconds = (long)Math.Floor (ts.TotalSeconds),
                NanoSeconds = (long)ts.Milliseconds * 1000000L
            };
        }

        [DllImport ("/usr/lib/libSystem.dylib")]
        public unsafe static extern int kevent (int kq, KernelEvent* changeList, int nChanges, KernelEvent* eventList, int nEvents, TimeSpec* timeout);
    }
}