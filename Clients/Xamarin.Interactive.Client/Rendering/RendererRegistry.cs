//
// RendererRegistry.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Rendering
{
	class RendererRegistry
	{
		readonly TypeMap<Type> typeMap = new TypeMap<Type> ();
		readonly HashSet<Assembly> registeredAssemblies = new HashSet<Assembly> ();

		public virtual void Initialize ()
		{
			RegisterAssembly (typeof(RendererRegistry).Assembly);
			RegisterAssembly (GetType ().Assembly);
			RegisterAssembly (Assembly.GetEntryAssembly ());
		}

		public virtual void RegisterAssembly (Assembly assembly)
		{
			lock (registeredAssemblies) {
				if (registeredAssemblies.Contains (assembly))
					return;

				registeredAssemblies.Add (assembly);

				foreach (var type in  GetTypes (assembly)) {
					if (!typeof(IRenderer).IsAssignableFrom (type))
						continue;

					foreach (RendererAttribute attr in
						type.GetCustomAttributes (typeof (RendererAttribute)))
						typeMap.Add (attr.SourceType, attr.ExactMatchRequired, type);
				}
			}
		}

		protected virtual Type [] GetTypes (Assembly assembly)
			=> assembly.GetTypes ();

		public virtual IEnumerable<IRenderer> GetRenderers (object source)
		{
			if (source == null)
				yield break;

			foreach (var rendererType in typeMap.GetValues (source.GetType ()))
				yield return (IRenderer)Activator.CreateInstance (rendererType);
		}
	}
}