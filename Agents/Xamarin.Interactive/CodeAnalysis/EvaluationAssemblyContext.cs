//
// CompilationAssemblyContext.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.CodeAnalysis
{
    sealed class EvaluationAssemblyContext : IDisposable
    {
        const string TAG = nameof (EvaluationAssemblyContext);

        readonly Dictionary<string, AssemblyDefinition> assemblyMap
            = new Dictionary<string, AssemblyDefinition> (
                StringComparer.OrdinalIgnoreCase);

        readonly Dictionary<string, Assembly> netAssemblyMap
            = new Dictionary<string, Assembly> ();

        public Action<Assembly, AssemblyDefinition> AssemblyResolvedHandler { get; set; }

        public EvaluationAssemblyContext ()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }

        public void Dispose ()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= HandleAssemblyResolve;
            GC.SuppressFinalize (this);
        }

        Assembly HandleAssemblyResolve (object sender, ResolveEventArgs args)
        {
            Log.Verbose (TAG, $"Handling assembly resolve event for {args.Name}.");
            Log.Verbose (TAG, $".NET Assembly map contents: ");
            foreach (var key in netAssemblyMap.Keys)
                Log.Verbose (TAG, $"    {key}");

            Assembly netAssembly;
            if (netAssemblyMap.TryGetValue (new AssemblyName (args.Name).ToString (), out netAssembly))
                return netAssembly;

            AssemblyDefinition assembly;
            if (assemblyMap.TryGetValue (new AssemblyName (args.Name).Name, out assembly)) {
                if (args.RequestingAssembly?.ReflectionOnly == true) {
                    if (File.Exists (assembly.Content.Location))
                        return Assembly.ReflectionOnlyLoadFrom (assembly.Content.Location);
                    
                    if (assembly.Content.PEImage != null)
                        return Assembly.ReflectionOnlyLoad (assembly.Content.PEImage);

                    throw new Exception (
                        $"Could not reflection-only load assembly {args.Name}, location " +
                        "did not exist and PEImage was not sent.");
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

                if (loadedAsm == null)
                    throw new Exception ($"Could not load assembly {args.Name}, location did not " +
                        "exist and PEImage was not sent.");

                AssemblyResolvedHandler?.Invoke (loadedAsm, assembly);
                return loadedAsm;
            }

            return null;
        }

        public void AddRange (IEnumerable<AssemblyDefinition> assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException (nameof (assemblies));

            foreach (var assembly in assemblies) {
                if (assembly != null)
                    Add (assembly);
            }
        }

        public void Add (AssemblyDefinition assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException (nameof (assembly));

            assemblyMap [assembly.Name.Name] = assembly;
        }

        public void Add (Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException (nameof (assembly));

            var assemblyName = assembly.GetName ().ToString ();

            netAssemblyMap [assemblyName] = assembly;

            // Sometimes these assemblies are looked up w/o the FQAN--for example
            // when doing round-trip serialization via Newtonsoft.Json including
            // type names. See https://bugzilla.xamarin.com/show_bug.cgi?id=58801
            // for an example workbook that fails without this patch. Insert the
            // bare assembly name into the cache as well to deal with these cases.
            netAssemblyMap [assembly.GetName ().Name] = assembly;
        }
    }
}