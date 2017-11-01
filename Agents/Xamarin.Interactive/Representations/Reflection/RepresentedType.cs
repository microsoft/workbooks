//
// RepresentedType.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations.Reflection
{
	[Serializable]
	public sealed class RepresentedType : IRepresentedType, ISerializable, IObjectReference
	{
		static readonly ReaderWriterLockSlim resolveLock = new ReaderWriterLockSlim ();
		static readonly Dictionary<string, RepresentedType> fromStringMap
			= new Dictionary<string, RepresentedType> (StringComparer.OrdinalIgnoreCase);
		static readonly Dictionary<Type, RepresentedType> fromTypeMap = new Dictionary<Type, RepresentedType> ();

		static RepresentedType RegisterType (string typeName, Type type)
		{
			var resolved = new RepresentedType {
				Name = type?.ToSerializableName () ?? typeName
			};

			fromStringMap [resolved.Name] = resolved;

			if (type != null) {
				resolved.ResolvedType = type;
				fromTypeMap [type] = resolved;
			}

			return resolved;
		}

		public static RepresentedType Lookup (string typeName)
		{
			if (typeName == null)
				return null;

			resolveLock.EnterUpgradeableReadLock ();
			try {
				RepresentedType resolved;
				if (fromStringMap.TryGetValue (typeName, out resolved))
					return resolved;

				resolveLock.EnterWriteLock ();
				try {
					return RegisterType (typeName, GetType (typeName));
				} finally {
					resolveLock.ExitWriteLock ();
				}
			} finally {
				resolveLock.ExitUpgradeableReadLock ();
			}
		}

		public static RepresentedType Lookup (Type type)
		{
			if (type == null)
				return null;

			resolveLock.EnterUpgradeableReadLock ();
			try {
				RepresentedType resolved;
				if (fromTypeMap.TryGetValue (type, out resolved))
					return resolved;

				resolveLock.EnterWriteLock ();
				try {
					return RegisterType (null, type);
				} finally {
					resolveLock.ExitWriteLock ();
				}
			} finally {
				resolveLock.ExitUpgradeableReadLock ();
			}
		}

		RepresentedType ()
		{
		}

		RepresentedType (SerializationInfo info, StreamingContext context)
		{
			Name = info.GetString ("Name");
		}

		void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context)
		{
			info.AddValue ("Name", Name);
		}

		object IObjectReference.GetRealObject (StreamingContext context)
		{
			return RepresentedType.Lookup (Name);
		}

		readonly Dictionary<string, RepresentedMemberInfo> proxyableMembers = new Dictionary<string, RepresentedMemberInfo> ();
		bool resolvedProxyableMembers;

		public string Name { get; private set; }
		public Type ResolvedType { get; private set; }
		public bool IsISerializable { get; private set; }
		public ConstructorInfo ISerializableCtor { get; private set; }

		IRepresentedType IRepresentedType.BaseType => BaseType;
		public RepresentedType BaseType => Lookup (ResolvedType?.BaseType);

		public IReadOnlyDictionary<string, RepresentedMemberInfo> ProxyableMembers {
			get {
				lock (proxyableMembers) {
					if (!resolvedProxyableMembers) {
						resolvedProxyableMembers = true;
						if (ResolvedType != null)
							ResolveProxyableMembers ();
					}

					return proxyableMembers;
				}
			}
		}

		const BindingFlags memberInfoBindingFlags =
			BindingFlags.Public |
			BindingFlags.Instance |
			BindingFlags.DeclaredOnly;

		void ResolveProxyableMembers ()
		{
			// Note: Type.FindMembers has a suboptimal implementation wherein it allocates
			// arrays for each MemberTypes value, calls the Get(Properties|Fields|etc) methods,
			// to populate the arrays, then allocates a final array to merge all MemberTypes
			// arrays, which we would then use here to populate proxyableMembers...
			// so instead we implement just what we need here to avoid that excess!
			// -abock, 2015-12-22

			foreach (var pi in ResolvedType.GetProperties (memberInfoBindingFlags)) {
				if (!pi.IsSpecialName && pi.GetIndexParameters ()?.Length == 0)
					proxyableMembers.Add (pi.Name, new RepresentedMemberInfo (this, pi));
			}

			foreach (var fi in ResolvedType.GetFields (memberInfoBindingFlags)) {
				if (!fi.IsSpecialName)
					proxyableMembers.Add (fi.Name, new RepresentedMemberInfo (this, fi));
			}
		}

		public static Type GetType (string typeName)
		{
			// regular Type.GetType attempts to resolve generic type arguments
			// only on the assembly of the type itself. For example, List`1[T],
			// where List`1 is in mscorlib, will attempt to resolve T only
			// in mscorlib!
			return Type.GetType (typeName, AssemblyResolver, TypeResolver, false, true);
		}

		static Assembly AssemblyResolver (AssemblyName assemblyName)
		{
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
				var loadedName = asm.GetName ();
				if (loadedName.Name == assemblyName.Name)
					return asm;
				else if (loadedName.Name.StartsWith ("Xamarin.Interactive.", StringComparison.OrdinalIgnoreCase))
					return typeof(RepresentedType).Assembly;
			}

			throw new Exception ("XipType.AssemblyResolver: unable to resolve assembly: " + assemblyName);
		}

		static Type TypeResolver (Assembly assembly, string typeName, bool ignoreCase)
		{
			// searches only executing assembly and mscorlib, unless typeName is assembly qualified
			var type = Type.GetType (typeName, false, ignoreCase);
			if (type != null)
				return type;

			// otherwise search all loaded assemblies...
			// FIXME: we may want to cache this and listen for new ones, but
			// typically the fast path above will be taken, so not introducing
			// that complexity now...
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
				type = asm.GetType (typeName, false, ignoreCase);
				if (type != null)
					return type;
			}

			return null;
		}

	}
}