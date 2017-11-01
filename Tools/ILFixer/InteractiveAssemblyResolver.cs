using System;
using System.Reflection;
using Mono.Cecil;

namespace ILFixer.DotNetCore
{
	class InteractiveAssemblyResolver : IAssemblyResolver
	{
		public virtual AssemblyDefinition Resolve (string fullName)
			=> Resolve (fullName, new ReaderParameters ());

		public virtual AssemblyDefinition Resolve (string fullName, ReaderParameters parameters)
		{
			if (fullName == null)
				throw new ArgumentNullException ("fullName");

			return Resolve (AssemblyNameReference.Parse (fullName), parameters);
		}

		public AssemblyDefinition Resolve (AssemblyNameReference name)
			=> Resolve (name, new ReaderParameters ());

		public AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			var loadedAsm = AppDomain.CurrentDomain.Load (new AssemblyName {
				Name = name.Name,
				Version = name.Version
			});

			if (loadedAsm != null)
				return AssemblyDefinition.ReadAssembly (loadedAsm.Location, new ReaderParameters () {
					AssemblyResolver = this
				});

			throw new AssemblyResolutionException (name);
		}

		public void Dispose ()
		{
		}
	}
}
