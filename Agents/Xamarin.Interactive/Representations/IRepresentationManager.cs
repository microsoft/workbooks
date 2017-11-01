//
// IRepresentationManager.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Representations
{
	public interface IRepresentationManager
	{
		void AddProvider (RepresentationProvider provider);
		void AddProvider (string typeName, Func<object, object> handler);
		void AddProvider<T> (Func<T, object> handler);
	}
}