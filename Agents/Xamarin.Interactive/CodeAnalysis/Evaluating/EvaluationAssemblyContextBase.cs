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

        protected Dictionary<string, AssemblyDefinition> AssemblyMap { get; }
            = new Dictionary<string, AssemblyDefinition> (
                StringComparer.OrdinalIgnoreCase);

        protected Dictionary<AssemblyName, Assembly> NetAssemblyMap { get; }
            = new Dictionary<AssemblyName, Assembly> (
                AssemblyNameInsensitiveNameOnlyComparer.Default);

        protected Dictionary<string, FilePath> ExternalDependencyMap { get; }
            = new Dictionary<string, FilePath> (
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

            AssemblyMap [assembly.Name.Name] = assembly;
        }

        public void Add (Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException (nameof (assembly));

            NetAssemblyMap [assembly.GetName ()] = assembly;
        }

        public void Add (AssemblyDependency externalDependency)
        {
            ExternalDependencyMap [externalDependency.Location.Name] = externalDependency.Location;
        }
    }
}