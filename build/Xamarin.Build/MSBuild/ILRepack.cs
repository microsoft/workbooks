// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using ILRepacking;

using Mono.Cecil;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class ILRepack : Task, ILRepacking.ILogger
    {
        [Required]
        public string [] InputAssemblies { get; set; } = Array.Empty<string> ();

        public string [] SearchDirectories { get; set; } = Array.Empty<string> ();

        public string [] References { get; set; } = Array.Empty<string> ();

        public string OutputAssembly { get; set; }

        public ITaskItem [] NamespaceRenames { get; set; } = Array.Empty<ITaskItem> ();

        public override bool Execute ()
        {
            var basePath = Path.GetDirectoryName (BuildEngine.ProjectFileOfTaskNode);

            IEnumerable<string> ExpandPath (IEnumerable<string> paths)
                => paths.Select (p => Path.GetFullPath (Path.Combine (basePath, p)));

            var searchPaths = new List<string> (ExpandPath (SearchDirectories));
            var inputAssemblies = new List<string> (ExpandPath (InputAssemblies));
            var outputAssembly = OutputAssembly ?? InputAssemblies [0];

            foreach (var reference in ExpandPath (References)) {
                var searchPath = Path.GetDirectoryName (reference);

                if (!IsNetstandardLibrary (reference) &&
                    !inputAssemblies.Contains (reference, StringComparer.OrdinalIgnoreCase))
                    inputAssemblies.Add (reference);
                else if (!searchPaths.Contains (searchPath, StringComparer.OrdinalIgnoreCase))
                    searchPaths.Add (searchPath);
            }

            Log.LogMessage (MessageImportance.Normal, "Output Assembly: {0}", outputAssembly);
            Log.LogMessage (MessageImportance.Normal, "Internalize: {0}", outputAssembly);

            foreach (var searchPath in searchPaths)
                Log.LogMessage (MessageImportance.Normal, "Search Path: {0}", searchPath);

            foreach (var inputAssembly in inputAssemblies)
                Log.LogMessage (MessageImportance.Normal, "Input Assembly: {0}", inputAssembly);

            var resolver = new DefaultAssemblyResolver ();
            foreach (var searchPath in searchPaths)
                resolver.AddSearchDirectory (searchPath);

            var readerParameters = new ReaderParameters {
                AssemblyResolver = resolver,
                ReadingMode = ReadingMode.Immediate
            };

            var renames = new List<(Regex regex, string replacement)> ();
            foreach (var rename in NamespaceRenames)
                renames.Add ((new Regex (rename.ItemSpec), rename.GetMetadata ("Replacement")));

            var typesToRenamespace = new HashSet<string> ();
            var renamespaceMap = new Dictionary<string, string> ();

            for (int i = 1; i < inputAssemblies.Count; i++) {
                using (var assemblyDefinition = AssemblyDefinition.ReadAssembly (inputAssemblies [i], readerParameters)) {
                    foreach (var typeDefinition in assemblyDefinition.MainModule.Types) {
                        typesToRenamespace.Add (typeDefinition.FullName);
                        if (renamespaceMap.ContainsKey (typeDefinition.Namespace))
                            continue;

                        var newNamespace = typeDefinition.Namespace;
                        foreach (var rename in renames)
                            newNamespace = rename.regex.Replace (newNamespace, rename.replacement);

                        if (newNamespace == typeDefinition.Namespace)
                            renamespaceMap.Add (typeDefinition.Namespace, $"<{Guid.NewGuid ()}>{newNamespace}");
                        else
                            renamespaceMap.Add (typeDefinition.Namespace, newNamespace);
                    }
                }
            }

            var options = new RepackOptions {
                InputAssemblies = inputAssemblies.ToArray (),
                SearchDirectories = searchPaths,
                Internalize = true,
                OutputFile = outputAssembly
            };

            var repack = new ILRepacking.ILRepack (options, this);

            try {
                repack.Repack ();
            } catch (DllNotFoundException) {
            }

            var renamedAssembly = outputAssembly + ".renamed";

            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly (outputAssembly, readerParameters)) {
                foreach (var typeDefinition in CollectTypeDefinitions (assemblyDefinition)) {
                    if (!typeDefinition.IsPublic && typesToRenamespace.Contains (typeDefinition.FullName))
                        typeDefinition.Namespace = renamespaceMap [typeDefinition.Namespace];
                }

                assemblyDefinition.Write (renamedAssembly);
            }

            File.Delete (outputAssembly);
            File.Move (renamedAssembly, outputAssembly);

            return true;
        }

        static bool IsNetstandardLibrary (string assemblyPath)
        {
            if (File.Exists (Path.Combine (Path.GetDirectoryName (assemblyPath), "netstandard.dll")))
                return true;

            if (Path.GetFileName (Path.GetDirectoryName (Path.GetDirectoryName (assemblyPath))) == "ref")
                return true;

            return false;
        }

        static HashSet<TypeDefinition> CollectTypeDefinitions (AssemblyDefinition assemblyDefinition)
        {
            var allTypes = new HashSet<TypeDefinition> ();
            CollectTypes (assemblyDefinition.Modules.SelectMany (m => m.Types));
            return allTypes;

            void CollectTypes (IEnumerable<TypeDefinition> types)
            {
                foreach (var type in types) {
                    if (allTypes.Add (type) && type.HasNestedTypes)
                        CollectTypes (type.NestedTypes);
                }
            }
        }

        #region ILRepacking.ILogger

        bool ILRepacking.ILogger.ShouldLogVerbose {
            get => true;
            set { }
        }

        void ILRepacking.ILogger.DuplicateIgnored (string ignoredType, object ignoredObject)
        {
        }

        void ILRepacking.ILogger.Log (object str)
            => throw new NotImplementedException ();

        void ILRepacking.ILogger.Info (string msg)
            => Log.LogMessage (MessageImportance.Low, msg);

        void ILRepacking.ILogger.Verbose (string msg)
            => Log.LogMessage (MessageImportance.Low, msg);

        void ILRepacking.ILogger.Warn (string msg)
        {
            if (msg.IndexOf (
                "Did not write source server data to output assembly",
                StringComparison.OrdinalIgnoreCase) < 0)
                Log.LogWarning (msg);
        }

        void ILRepacking.ILogger.Error (string msg)
            => Log.LogError (msg);

        #endregion
    }
}