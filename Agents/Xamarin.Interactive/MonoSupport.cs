//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive
{
    static class MonoSupport
    {
        [DllImport ("__Internal")]
        static extern IntPtr mono_assembly_get_image (IntPtr assembly);

        [DllImport ("__Internal")]
        static extern void mono_dllmap_insert (
            IntPtr image,
            string dll,
            string func,
            string tdll,
            string tfunc);

        public static void AddDllMapEntries (
            Assembly assembly,
            AssemblyDependency externalDependency)
        {
            var monoAssemblyPointerField = assembly.GetType ().GetField (
                "_mono_assembly",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (monoAssemblyPointerField == null)
                return;

            var monoAssemblyPointer = (IntPtr)monoAssemblyPointerField.GetValue (assembly);
            var monoImage = mono_assembly_get_image (monoAssemblyPointer);

            var location = externalDependency.Location;

            // If the P/Invoke path is an exact match (libfoo.dylib)
            mono_dllmap_insert (monoImage, location.Name, null, location, null);

            // If the P/Invoke is written as libfoo.dll.
            var dllPath = Path.ChangeExtension (location.Name, ".dll");
            mono_dllmap_insert (monoImage, dllPath, null, location, null);

            // If the P/Invoke is written as libfoo
            var dllPathWithoutExtension = Path.GetFileNameWithoutExtension (location);
            mono_dllmap_insert (monoImage, dllPathWithoutExtension, null, location, null);

            // If the P/Invoke is written as foo.dll or foo
            if (dllPath.StartsWith ("lib", StringComparison.OrdinalIgnoreCase)) {
                var nonLibPath = dllPath.Substring (3);
                var nonLibWithoutExtension = dllPathWithoutExtension.Substring (3);
                mono_dllmap_insert (monoImage, nonLibPath, null, location, null);
                mono_dllmap_insert (monoImage, nonLibWithoutExtension, null, location, null);
            }
        }
    }
}
