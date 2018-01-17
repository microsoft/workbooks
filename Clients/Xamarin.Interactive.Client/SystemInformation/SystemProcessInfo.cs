//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Xamarin.Interactive.SystemInformation
{
    sealed class SystemProcessInfo
    {
        public int Pid { get; }
        public string ExecPath { get; }
        public IReadOnlyList<string> Arguments { get; }
        public IReadOnlyDictionary<string, string> Environment { get; }

        SystemProcessInfo (
            int pid,
            string execPath,
            IReadOnlyList<string> arguments,
            IReadOnlyDictionary<string, string> environment)
        {
            Pid = pid;
            ExecPath = execPath;
            Arguments = arguments;
            Environment = environment;
        }

        static void EnsureBsd ()
        {
            if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                throw new PlatformNotSupportedException ("not supported on non-Unix/BSD systems");
        }

        public static IReadOnlyList<int> GetAllProcessIds ()
        {
            EnsureBsd ();
            return BsdGetAllProcessIds ();
        }

        public static IEnumerable<SystemProcessInfo> GetAllProcesses (IReadOnlyList<int> processIds)
        {
            EnsureBsd ();
            return BsdGetAllProcesses (processIds);
        }

        public static bool TryGetProcess (int processId, out SystemProcessInfo processInfo)
        {
            processInfo = GetAllProcesses (new [] { processId }).FirstOrDefault ();
            return processInfo != null;
        }

        public static IEnumerable<SystemProcessInfo> GetAllProcesses ()
            => GetAllProcesses (GetAllProcessIds ());

        #region macOS/BSD

        public sealed class ErrnoException : Exception
        {
            [DllImport ("libc")]
            static extern string strerror (int errno);

            public int Errno { get; }

            public ErrnoException () : this (Marshal.GetLastWin32Error ())
            {
            }

            public ErrnoException (int errno) : base ($"ERRNO {errno}: {strerror (errno)}")
            {
                Errno = errno;
            }
        }

        [DllImport ("libc", SetLastError = true)]
        static extern unsafe int sysctl (
            int [] name, int namelen,
            IntPtr oldp, ref IntPtr oldlenp,
            IntPtr newp, IntPtr newlen);

        [StructLayout (LayoutKind.Sequential)]
        struct kinfo_proc
        {
            public IntPtr p_un_0;
            public IntPtr p_un_1;
            public IntPtr p_vmspace;
            public IntPtr p_sigacts;
            public int p_flag;
            public byte p_stat;
            public int p_pid;
            // other fields follow but we are not interested in them
        }

        const int ENOENT = 2;
        const int ESRCH = 3;
        const int ENOMEM = 12;
        const int EINVAL = 22;

        const int CTL_KERN = 1;
        const int KERN_ARGMAX = 8;
        const int KERN_PROC = 14;
        const int KERN_PROC_ALL = 0;
        const int KERN_PROCARGS = 38;
        const int KERN_PROCARGS2 = 49;
        const int KERN_PROCNAME = 62;

        static IReadOnlyList<int> BsdGetAllProcessIds ()
        {
            const int sizeof_kinfo_proc = 648; // macOS 10.12 x86_64

            int [] kernProcAll = {
                CTL_KERN,
                KERN_PROC,
                KERN_PROC_ALL
            };

            while (true) {
                var length = IntPtr.Zero;
                if (sysctl (kernProcAll, kernProcAll.Length, IntPtr.Zero, ref length, IntPtr.Zero, IntPtr.Zero) != 0)
                    throw new ErrnoException ();

                length = new IntPtr ((long)length);

                var result = Marshal.AllocHGlobal (length);

                if (sysctl (kernProcAll, kernProcAll.Length, result, ref length, IntPtr.Zero, IntPtr.Zero) != 0) {
                    var errno = Marshal.GetLastWin32Error ();
                    if (errno == ENOMEM) {
                        Marshal.FreeHGlobal (result);
                        continue;
                    }

                    throw new ErrnoException (errno);
                }

                try {
                    var pids = new int [(int)((long)length / sizeof_kinfo_proc)];

                    for (int i = 0; i < pids.Length; i++) {
                        var kinfo_proc = Marshal.PtrToStructure<kinfo_proc> (
                            IntPtr.Add (result, i * sizeof_kinfo_proc));
                        pids [i] = kinfo_proc.p_pid;
                    }

                    return pids;
                } finally {
                    Marshal.FreeHGlobal (result);
                }
            }
        }

        static unsafe int KernelArgmax ()
        {
            int [] argMaxMib = {
                CTL_KERN,
                KERN_ARGMAX
            };

            int argmax = 0;
            var argmaxPtr = new IntPtr (&argmax);
            var argmaxSize = new IntPtr (sizeof (int));

            if (sysctl (
                argMaxMib, argMaxMib.Length,
                argmaxPtr, ref argmaxSize,
                IntPtr.Zero, IntPtr.Zero) != 0)
                throw new ErrnoException ();

            return argmax;
        }

        static unsafe int KernelProcArgs2 (int pid, int maxProcArgsSize, byte [] procArgs)
        {
            int [] procArgsMib = {
                CTL_KERN,
                KERN_PROCARGS2,
                pid
            };

            var procArgsSize = new IntPtr (maxProcArgsSize);
            fixed (byte *procArgsPtr = procArgs) {
                if (sysctl (
                    procArgsMib, procArgsMib.Length,
                    new IntPtr (procArgsPtr), ref procArgsSize,
                    IntPtr.Zero, IntPtr.Zero) != 0) {
                    var errno = Marshal.GetLastWin32Error ();
                    switch (errno) {
                    case ENOENT:
                    case EINVAL:
                    case ESRCH:
                        // process no longer exists/unable to read process info (e.g. permissions)
                        return -1;
                    default:
                        throw new ErrnoException (errno);
                    }
                }
            }

            return procArgsSize.ToInt32 ();
        }

        static IEnumerable<SystemProcessInfo> BsdGetAllProcesses (IReadOnlyList<int> processIds)
        {
            if (processIds == null)
                throw new ArgumentNullException (nameof (processIds));

            var maxProcArgsSize = KernelArgmax ();
            var procArgs = new byte [maxProcArgsSize];

            foreach (var pid in processIds) {
                var procArgsSize = KernelProcArgs2 (pid, maxProcArgsSize, procArgs);
                if (procArgsSize <= 0)
                    continue;

                string execPath = null;
                var args = new List<string> ();
                var environment = new Dictionary<string, string> ();

                // KERN_PROCARGS2 is not very well documented, but it appears to first contain argc as
                // a 32-bit integer, followed by records delimited by ASCII control characters.
                //
                // I believe the records that follow argc are laid out as such (literally a copy of the
                // memory from the process' address space):
                //
                //   0: exec path (e.g. '/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono')
                //   1 .. 1 + argc: arguments passed to exec
                //   1 + argc .. (end of buffer): environment variables passed to exec

                var argc = BitConverter.ToInt32 (procArgs, 0);
                var strStart = sizeof (int);

                for (int i = strStart, n = procArgsSize; i < n; i++) {
                    var strLength = i - strStart;
                    if (procArgs [i] == 0 && strLength > 0) {
                        var arg = Encoding.UTF8.GetString (procArgs, strStart, strLength);

                        if (execPath == null) {
                            execPath = arg;
                        } else if (args.Count == argc) {
                            var envDelim = arg.IndexOf ('=');
                            if (envDelim > 0)
                                environment.Add (
                                    arg.Substring (0, envDelim),
                                    arg.Substring (envDelim + 1));
                        } else {
                            args.Add (arg);
                        }

                        strStart = i + 1;
                    } else if (Char.IsControl ((char)procArgs [i])) {
                        strStart = i + 1;
                    }
                }

                yield return new SystemProcessInfo (
                    pid,
                    execPath,
                    args,
                    environment);
            }
        }

        #endregion
    }
}