//
// AgentFeatures.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Linq;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class AgentFeatures : IEquatable<AgentFeatures>
	{
		public string [] SupportedViewInspectionHierarchies { get; }

		public AgentFeatures (string [] supportedViewInspectionHierarchies)
		{
			SupportedViewInspectionHierarchies = supportedViewInspectionHierarchies;
		}

		public bool Equals (AgentFeatures obj)
		{
			if (obj == null)
				return false;

			if (Object.ReferenceEquals (this, obj))
				return true;

			if (Object.ReferenceEquals (
				SupportedViewInspectionHierarchies,
				obj.SupportedViewInspectionHierarchies))
				return true;

			return Enumerable.SequenceEqual (
				SupportedViewInspectionHierarchies,
				obj.SupportedViewInspectionHierarchies);
		}

		public override bool Equals (object obj)
			=> Equals (obj as AgentFeatures);

		public override int GetHashCode ()
			=> SupportedViewInspectionHierarchies == null
				? 0
				: SupportedViewInspectionHierarchies.GetHashCode ();
	}
}