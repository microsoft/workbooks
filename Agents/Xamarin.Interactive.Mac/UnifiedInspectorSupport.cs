//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.IO;
using System.Reflection;

using Xamarin.Interactive;
using Xamarin.Interactive.Core;

#if IOS
using UnifiedAgent = Xamarin.Interactive.iOS.iOSAgent;
#elif MAC
using UnifiedAgent = Xamarin.Interactive.Mac.MacAgent;
#endif

namespace Xamarin
{
    // WARNING: this type must not _directly_ reference any type in the PCL!
    // Doing so will break live inspection since this type implements the
    // PCL loading (so _directly_ referencing a type in the PCL will cause
    // the debugger to fail to load the type because the loader cannot load)
    public static partial class InspectorSupport
    {
        #if IOS
        const string netProfilesPath = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono";
        #elif MAC
        const string netProfilesPath = "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono";
        #endif

        static bool environmentDetected;
        static AssemblyName pclInteractiveAssemblyName;
        static string fxPath;

        static IntPtr breakdanceTimerSource;

        internal static void StartBreakdance ()
        {
            breakdanceTimerSource = Dispatch.ScheduleRepeatingTimer (TimeSpan.FromSeconds (1),
                userdata => BreakdanceStep ());
        }

        static void StopBreakdance ()
        {
            if (breakdanceTimerSource != IntPtr.Zero) {
                Dispatch.Cancel (breakdanceTimerSource);
                breakdanceTimerSource = IntPtr.Zero;
            }
        }

        static partial void CreateAgent (AgentStartOptions startOptions)
        {
            StopBreakdance ();

            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;

            var source = IntPtr.Zero;
            source = Dispatch.ScheduleRepeatingTimer (TimeSpan.FromSeconds (0), userdata => {
                Dispatch.Cancel (source);
                agent = new UnifiedAgent ().Start (startOptions);
            });
        }

        static void DetectEnvironment ()
        {
            if (environmentDetected)
                return;

            environmentDetected = true;

            var runningCorlibVersion = typeof (object).Assembly.GetName ().Version;

            foreach (var profilePath in new DirectoryInfo (netProfilesPath).EnumerateDirectories ()) {
                var corlibPath = Path.Combine (profilePath.FullName, "mscorlib.dll");
                if (File.Exists (corlibPath) &&
                    AssemblyName.GetAssemblyName (corlibPath)?.Version == runningCorlibVersion) {
                    fxPath = profilePath.FullName;
                    break;
                }
            }

            pclInteractiveAssemblyName = Assembly
                .GetExecutingAssembly ()
                .GetReferencedAssemblies ()
                .FirstOrDefault (a => a.Name == "Xamarin.Interactive");
        }

        static Assembly HandleAssemblyResolve (object sender, ResolveEventArgs e)
        {
            DetectEnvironment ();

            if (pclInteractiveAssemblyName == null || !Directory.Exists (fxPath))
                return null;

            var requestingAssemblyName = e.RequestingAssembly?.GetName ();

            if (requestingAssemblyName?.FullName != pclInteractiveAssemblyName.FullName)
                return null;

            var assemblyFileName = new AssemblyName (e.Name).Name + ".dll";

            var assemblyPath = Path.Combine (fxPath, assemblyFileName);
            if (!File.Exists (assemblyPath)) {
                assemblyPath = Path.Combine (fxPath, "Facades", assemblyFileName);
                if (!File.Exists (assemblyPath))
                    return null;
            }

            return Assembly.LoadFrom (assemblyPath);
        }
    }
}