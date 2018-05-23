//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.IO;
using System.Runtime.Loader;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.DotNetCore
{
    sealed class NetCoreEvaluationAssemblyContext : EvaluationAssemblyContextBase
    {
        const string TAG = nameof (NetCoreEvaluationAssemblyContext);

        readonly InteractiveAssemblyLoadContext assemblyLoadContext;

        sealed class InteractiveAssemblyLoadContext : AssemblyLoadContext
        {
            const string TAG = nameof (InteractiveAssemblyLoadContext);

            NetCoreEvaluationAssemblyContext evaluationAssemblyContext;

            public InteractiveAssemblyLoadContext (NetCoreEvaluationAssemblyContext evaluationAssemblyContext)
                => this.evaluationAssemblyContext = evaluationAssemblyContext;

            protected override Assembly Load (AssemblyName assemblyName)
            {
                Log.Info (TAG, $"Requested assembly load for {assemblyName}.");

                if (evaluationAssemblyContext.CompilationAssemblyMap.TryGetValue (assemblyName, out var netAssembly))
                    return netAssembly;

                if (evaluationAssemblyContext.ReferencedAssemblyMap.TryGetValue (assemblyName, out var assemblyDefinition))
                    return LoadAssemblyFromAssemblyDefinition (assemblyName, assemblyDefinition);

                Log.Warning (
                    TAG,
                    $"Could not load assembly {assemblyName}, it wasn't present in any list of assemblies.");

                return null;
            }

            Assembly LoadAssemblyFromAssemblyDefinition (AssemblyName assemblyName, AssemblyDefinition assemblyDefinition)
            {
                Assembly loadedAsm;

                if (File.Exists (assemblyDefinition.Content.Location)) {
                    loadedAsm = LoadFromAssemblyPath (assemblyDefinition.Content.Location);
                    Log.Info (TAG, $"Loaded assembly {loadedAsm} from {assemblyDefinition.Content.Location}.");
                } else if (assemblyDefinition.Content.PEImage != null) {
                    var imageStream = new MemoryStream (assemblyDefinition.Content.PEImage);
                    if (assemblyDefinition.Content.DebugSymbols != null) {
                        var symStream = new MemoryStream (assemblyDefinition.Content.DebugSymbols);
                        loadedAsm = LoadFromStream (imageStream, symStream);
                    }
                    loadedAsm = LoadFromStream (imageStream);
                    Log.Info (TAG, $"Loaded assembly {loadedAsm} from sent PE image.");
                } else {
                    Log.Warning (
                        TAG,
                        $"Could not load assembly {assemblyName}, location {assemblyDefinition.Content.Location} did not " +
                        "exist and PEImage was not sent.");
                    return null;
                }

                evaluationAssemblyContext.AssemblyResolvedHandler?.Invoke (loadedAsm, assemblyDefinition);

                return loadedAsm;
            }

            internal Assembly InternalLoadByName (AssemblyName assemblyName)
                => Load (assemblyName);

            protected override IntPtr LoadUnmanagedDll (string unmanagedDllName)
            {
                Log.Info (TAG, $"Requested unmanaged DLL load for {unmanagedDllName}.");

                if (!evaluationAssemblyContext.ExternalDependencyMap.ContainsKey (unmanagedDllName)) {
                    Log.Info (TAG, $"Don't know where to load this from, falling back to base.");
                    return base.LoadUnmanagedDll (unmanagedDllName);
                }

                string unmanagedPath = evaluationAssemblyContext.ExternalDependencyMap [unmanagedDllName];
                Log.Info (TAG, $"Loading unmanaged DLL {unmanagedDllName} from {unmanagedPath}.");

                var unmanagedHandle = LoadUnmanagedDllFromPath (unmanagedPath);
                Log.Info (TAG, $"Loaded unmanaged DLL {unmanagedDllName} from {unmanagedPath}, handle is {unmanagedHandle}.");

                return unmanagedHandle;
            }

            public IntPtr LoadExternalDependency (AssemblyDependency dependency)
                => LoadUnmanagedDllFromPath (dependency.Location.FullPath);
        }

        public NetCoreEvaluationAssemblyContext ()
        {
            assemblyLoadContext = new InteractiveAssemblyLoadContext (this);
            AssemblyLoadContext.Default.Resolving += HandleAssemblyResolve;
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
            AssemblyLoadContext.Default.Resolving -= HandleAssemblyResolve;
        }

        public IntPtr LoadExternalDependency (AssemblyDependency dependency)
            => assemblyLoadContext.LoadExternalDependency (dependency);

        Assembly HandleAssemblyResolve (AssemblyLoadContext loadContext, AssemblyName assemblyName)
            => assemblyLoadContext.InternalLoadByName (assemblyName);
    }
}