//
// ExternalDependency.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Reflection
{
	abstract class ExternalDependency
	{
		public FilePath Location { get; }

		protected ExternalDependency (FilePath location)
		{
			Location = location;
		}
	}
}