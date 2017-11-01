//
// MonoScriptCompilationPatcher.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;

using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Compilation.Roslyn
{
	/// <summary>
	/// Patches a ScriptCompilation's metadata and PE image to ensure the assembly name and any
	/// assemblies referenced will be unique when executed on a Mono runtime since there is a
	/// long-standing bug in Mono where it will not load an assembly more than once with the same
	/// name. This is a problem for re-evaluating existing submissions. Rather than completely
	/// rewrite our Roslyn project model to accomodate the buggy runtime, we patch the executable
	/// images as an isolated compilation pass until runtime can be fixed.
	/// </summary>
	/// <remarks>
	/// The patching mechanism is incredibly aggressive. Ideally it would use SRM, but SRM does not
	/// support writing at all or even getting byte offsets from strings yet (e.g. we have no way
	/// of knowing where in the source PE image a StringHandle's data is located). Therefore, it
	/// relies on an 8 byte magic value. We use a unicode monkey as the first four bytes of magic
	/// followed by ' XIS' (Xamarin Inspector Submission) as the last four bytes of magic. The
	/// last four bytes will be patched/rewritten to be a ToString'd number (0000-9999) representing
	/// the number of times that submission has been re-evaluated, thus making the resulting assembly
	/// for a submission always unique (until the 10000th edit explodes the world).
	/// 
	/// 8 bytes of magic is chosen simply to perform fixed ulong compares by dereferencing a byte
	/// pointer since we have to check every single byte in the image (unless we find a match,
	/// in which case we can skip ahead a little to perform the next check).
	/// 
	/// We need to fix the Mono runtime.
	/// </remarks>
	sealed class MonoScriptCompilationPatcher
	{
		unsafe delegate void PatchHandler (byte* bytes, int byteCount);

		readonly byte [] assemblyNamePrefixBytes;
		ImmutableDictionary<DocumentId, int> documentVersions = ImmutableDictionary<DocumentId, int>.Empty;
		ImmutableDictionary<string, int> referenceRewrites = ImmutableDictionary<string, int>.Empty;

		public MonoScriptCompilationPatcher (byte [] assemblyNamePrefixBytes)
		{
			this.assemblyNamePrefixBytes = assemblyNamePrefixBytes;
		}

		public unsafe AssemblyDefinition Patch (
			DocumentId submissionDocumentId,
			AssemblyDefinition remoteAssembly)
		{
			var image = remoteAssembly?.Content.PEImage;
			if (image == null || image.Length == 0 || image.Length < assemblyNamePrefixBytes.Length + 1)
				return remoteAssembly;

			int documentVersion;
			documentVersions.TryGetValue (submissionDocumentId, out documentVersion);
			documentVersions = documentVersions.SetItem (submissionDocumentId, documentVersion + 1);

			var isAssemblyPatched = false;

			Patch (image, assemblyNamePrefixBytes, (bytes, byteCount) => {
				var name = Encoding.UTF8.GetString (bytes, byteCount);
				int patchVersion;
				var isAssemblyName = false;

				if (name == remoteAssembly.Name.FullName) {
					// patch version is the submission document version when
					// rewriting references to the submission assembly itself
					patchVersion = documentVersion;
					isAssemblyName = true;
				} else
					// otherwise we should have seen this reference from a 
					// previous patch, so get the patch number from the map
					referenceRewrites.TryGetValue (name, out patchVersion);

				// apply the patch into the image
				var patch = patchVersion.ToString ("D4", CultureInfo.InvariantCulture);
				var patchBytes = Encoding.UTF8.GetBytes (patch);
				fixed (byte* patchBytesPtr = &patchBytes [0])
					*(uint*)(bytes + 4) = *(uint*)patchBytesPtr;

				if (isAssemblyName && !isAssemblyPatched) {
					// if we're patching the submission assembly itself, we need to
					// also fix up the ScriptCompilation metadata, and install the
					// patch reference for subsequent dependent submission assemblies
					var patchedName = Encoding.UTF8.GetString (bytes, byteCount);
					isAssemblyPatched = true;

					remoteAssembly = new AssemblyDefinition (
						new AssemblyName (patchedName),
						null,
						remoteAssembly.EntryPoint.TypeName.Replace (name, patchedName),
						remoteAssembly.EntryPoint.MethodName,
						image);

					referenceRewrites = referenceRewrites.SetItem (name, patchVersion);
				}
			});

			return remoteAssembly;
		}

		/// <summary>
		/// Search the target byte array for an 8-byte magic value, then rewrite the string prefixed
		/// by the magic value. Strings are terminated by '\0', '.', or '>'. This is very aggressive
		/// and has nothing to do with the PE format, so the magic value should be very unique!
		/// </summary>
		static unsafe void Patch (byte [] target, byte [] magicBytes, PatchHandler patchHandler)
		{
			if (magicBytes.Length != 8)
				throw new ArgumentException ("must be exactly 8 bytes in length", nameof (magicBytes));

			fixed (byte* magicPtr = &magicBytes [0])
			fixed (byte* targetPtr = &target [0]) {
				var magic = *(ulong*)magicPtr;
				for (var i = 0; i < target.Length - 8;) {
					var patchAddr = (ulong*)(targetPtr + i);
					if (*patchAddr != magic)
						i++;
					else {
						var length = 8;
						while (true) {
							var b = target [i + length];
							if (b == 0 || b == '.' || b == '>')
								break;
							length++;
						}

						patchHandler (targetPtr + i, length);
						i += length;
					}
				}
			}
		}
	}
}