//
// NativeDependency.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Reflection
{
	class NativeDependency : ExternalDependency
	{
		public string Name { get; }

		public NativeDependency (string name, FilePath location) : base (location)
		{
			Name = name;
		}
	}
}