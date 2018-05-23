//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    abstract class EvaluationAssemblyContextBase : IDisposable
    {
        const string TAG = nameof (EvaluationAssemblyContextBase);

        protected IDictionary<AssemblyName, AssemblyDefinition> ReferencedAssemblyMap { get; }
            = new SortedDictionary<AssemblyName, AssemblyDefinition> (
                AssemblyNameInsensitiveNameOnlyComparer.Default);

        protected IDictionary<AssemblyName, Assembly> CompilationAssemblyMap { get; }
            = new SortedDictionary<AssemblyName, Assembly> (
                AssemblyNameInsensitiveNameOnlyComparer.Default);

        protected IDictionary<string, FilePath> ExternalDependencyMap { get; }
            = new SortedDictionary<string, FilePath> (
                StringComparer.OrdinalIgnoreCase);

        public Action<Assembly, AssemblyDefinition> AssemblyResolvedHandler { get; set; }

        public EvaluationAssemblyContextBase (Action<Assembly, AssemblyDefinition> assemblyResolvedHandler = null)
            => AssemblyResolvedHandler = assemblyResolvedHandler;

        public void Dispose ()
            => Dispose (true);

        protected virtual void Dispose (bool disposing)
            => GC.SuppressFinalize (this);

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

            ReferencedAssemblyMap [assembly.Name] = assembly;
        }

        public void Add (Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException (nameof (assembly));

            CompilationAssemblyMap [assembly.GetName ()] = assembly;
        }

        public void Add (AssemblyDependency externalDependency)
        {
            ExternalDependencyMap [externalDependency.Name] = externalDependency.Location;
        }
    }
}