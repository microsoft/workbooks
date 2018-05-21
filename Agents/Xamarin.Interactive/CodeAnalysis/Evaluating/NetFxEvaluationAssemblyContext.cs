//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;

using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    sealed class NetFxEvaluationAssemblyContext : EvaluationAssemblyContextBase
    {
        const string TAG = nameof (NetFxEvaluationAssemblyContext);

        public NetFxEvaluationAssemblyContext ()
            => AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
            AppDomain.CurrentDomain.AssemblyResolve -= HandleAssemblyResolve;
        }

        Assembly HandleAssemblyResolve (object sender, ResolveEventArgs args)
        {
            Log.Verbose (TAG, $"Handling assembly resolve event for {args.Name}.");
            return LoadAssemblyFromName (args.Name, args.RequestingAssembly);
        }

        Assembly LoadAssemblyFromName (string assemblyName, Assembly requestingAssembly)
        {
            Assembly netAssembly;
            if (NetAssemblyMap.TryGetValue (new AssemblyName (assemblyName), out netAssembly))
                return netAssembly;

            AssemblyDefinition assembly;
            if (AssemblyMap.TryGetValue (new AssemblyName (assemblyName).Name, out assembly)) {
                if (requestingAssembly?.ReflectionOnly == true) {
                    if (File.Exists (assembly.Content.Location))
                        return Assembly.ReflectionOnlyLoadFrom (assembly.Content.Location);

                    if (assembly.Content.PEImage != null)
                        return Assembly.ReflectionOnlyLoad (assembly.Content.PEImage);

                    Log.Warning (
                        TAG,
                        $"Could not reflection-only load assembly {assemblyName}, location {assembly.Content.Location}" +
                        "did not exist and PEImage was not sent.");

                    return null;
                }

                Assembly loadedAsm;

                if (File.Exists (assembly.Content.Location))
                    loadedAsm = Assembly.LoadFrom (assembly.Content.Location);
                else if (assembly.Content.PEImage != null) {
                    if (assembly.Content.DebugSymbols != null)
                        loadedAsm = Assembly.Load (
                            assembly.Content.PEImage,
                            assembly.Content.DebugSymbols);
                    else
                        loadedAsm = Assembly.Load (assembly.Content.PEImage);
                } else
                    loadedAsm = null;

                if (loadedAsm == null) {
                    Log.Warning (
                        TAG,
                        $"Could not load assembly {assemblyName}, location {assembly.Content.Location} did not " +
                        "exist and PEImage was not sent.");
                    return null;
                }

                AssemblyResolvedHandler?.Invoke (loadedAsm, assembly);
                return loadedAsm;
            }

            return null;
        }
    }
}