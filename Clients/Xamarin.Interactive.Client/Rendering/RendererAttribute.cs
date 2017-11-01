//
// RendererAttribute.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive.Rendering
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public sealed class RendererAttribute : Attribute
	{
		public Type SourceType { get; }
		public bool ExactMatchRequired { get; }

		public RendererAttribute (Type sourceType, bool exactMatchRequired = true)
		{
			if (sourceType == null)
				throw new ArgumentNullException (nameof(sourceType));

			SourceType = sourceType;
			ExactMatchRequired = exactMatchRequired;
		}
	}
}