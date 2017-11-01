//
// NetStandardTool.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Linq;
using ILFixer.DotNetCore;
using Mono.Cecil;

namespace ILFixer
{
	static class NetStandardTool
	{
		public static int Run (string [] toolArgs)
		{
			if (toolArgs.Length < 1) {
				Console.WriteLine ("Usage: ilfixer netstandard PATH_TO_NETSTANDARD_DLL");
				return 1;
			}

			var netStandardAssemblyPath = toolArgs [0];

			// Remove the `netstandard` external and replace it with an `mscorlib` one.
			var assemblyDefinition = AssemblyDefinition.ReadAssembly (netStandardAssemblyPath, new ReaderParameters {
				AssemblyResolver = new InteractiveAssemblyResolver (),
			});
			var mscorlibRef = new AssemblyNameReference ("mscorlib", new Version (4, 0, 0, 0)) {
				PublicKeyToken = new byte [] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 }
			};

			var hasChanges = false;

			foreach (var module in assemblyDefinition.Modules) {
				var assemblyRefs = module.AssemblyReferences;
				var netStandardRef = assemblyRefs.SingleOrDefault (ar => ar.Name == "netstandard");

				if (netStandardRef == null) {
					Console.WriteLine ($"Module {module.FileName} did not have netstandard reference.");
					continue;
				}

				assemblyRefs.Remove (netStandardRef);
				assemblyRefs.Add (mscorlibRef);

				hasChanges = true;

				Console.WriteLine ($"Patched netstandard reference with mscorlib reference in module {module.FileName}");
			}

			if (!hasChanges)
				return 0;

			// Take a backup of the original assembly.
			var backupPath = Path.ChangeExtension (netStandardAssemblyPath, ".bak");
			File.Copy (netStandardAssemblyPath, backupPath, true);

			var fixedPath = Path.ChangeExtension (netStandardAssemblyPath, ".fixed");

			assemblyDefinition.Write (fixedPath);
			assemblyDefinition.Dispose ();

			File.Copy (fixedPath, netStandardAssemblyPath, true);
			File.Delete (fixedPath);

			return 0;
		}
	}
}