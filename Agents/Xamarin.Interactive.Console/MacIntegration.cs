//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.ConsoleAgent
{
    static class MacIntegration
    {
        const string TAG = nameof (MacIntegration);

        static MethodInfo nsApplicationInitMethod;
        static FieldInfo nsApplicationInitializedField;
        static Type nsUrlSessionHandlerType;
        static bool nsApplicationInitialized;
        static bool isMac;

        [DllImport ("libc")]
        public static extern IntPtr dlopen (string path, int mode);

        static MacIntegration ()
        {
            switch (Environment.OSVersion.Platform) {
            case PlatformID.Unix:
            case PlatformID.MacOSX:
                isMac = true;
                break;
            default:
                isMac = false;
                break;
            }
        }

        public static bool IsMac => isMac;

        public static void Integrate (Agent agent)
        {
            if (!IsMac)
                return;

            const string xmRoot = "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current";
            var arch = IntPtr.Size == 8 ? "x86_64" : "i386";
            var xmDylibPath = $"{xmRoot}/lib/libxammac.dylib";
            var xmAssemblyPath = $"{xmRoot}/lib/{arch}/full/Xamarin.Mac.dll";

            if (!File.Exists (xmAssemblyPath))
                return;

            try {
                if (dlopen (xmDylibPath, 0) == IntPtr.Zero)
                    return;

                var xmAssembly = Assembly.LoadFrom (xmAssemblyPath);
                if (xmAssembly == null)
                    return;

                var nsApplicationType = xmAssembly.GetType ("AppKit.NSApplication");
                if (nsApplicationType == null)
                    return;

                nsApplicationInitMethod = nsApplicationType.GetMethod (
                    "Init",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
                if (nsApplicationInitMethod == null)
                    return;

                // since this is private API, do not enforce it existing
                nsApplicationInitializedField = nsApplicationType.GetField (
                    "initialized",
                    BindingFlags.NonPublic | BindingFlags.Static);

                nsUrlSessionHandlerType = xmAssembly.GetType ("Foundation.NSUrlSessionHandler");
                if (nsUrlSessionHandlerType == null)
                    return;

                agent.CreateDefaultHttpMessageHandler = CreateNSUrlSessionHandler;
            } catch (Exception e) {
                Log.Error (TAG, "unable to initialize Xamarin.Mac integration", e);
            }
        }

        static object CreateNSUrlSessionHandler ()
        {
            NSApplicationInit ();
            return Activator.CreateInstance (nsUrlSessionHandlerType);
        }

        static void NSApplicationInit ()
        {
            if (nsApplicationInitialized)
                return;

            if (nsApplicationInitializedField != null)
                nsApplicationInitialized = (bool)nsApplicationInitializedField
                    .GetValue (null);

            if (!nsApplicationInitialized) {
                try {
                    nsApplicationInitMethod.Invoke (null, null);
                } catch (InvalidOperationException) {
                }
            }

            nsApplicationInitialized = true;
        }
    }
}