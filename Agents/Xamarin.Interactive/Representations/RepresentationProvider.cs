//
// RepresentationProvider.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Representations
{
	public abstract class RepresentationProvider
	{
		static readonly object [] emptyRepresentations = new object [0];

		public virtual bool HasSensibleEnumerator (IEnumerable enumerable)
			=> true;

		public virtual bool ShouldReflect (object obj)
			=> true;

		public virtual bool ShouldReadMemberValue (IRepresentedMemberInfo representedMemberInfo, object obj)
			=> true;

		public virtual IEnumerable<object> ProvideRepresentations (object obj)
			=> emptyRepresentations;

		public virtual bool TryConvertFromRepresentation (
			IRepresentedType representedType,
			object [] representations,
			out object represented)
		{
			represented = null;
			return false;
		}

		protected virtual bool TryFindMatchingRepresentation<TResolvedType, TRepresentationType> (
			IRepresentedType representedType,
			object [] representations,
			out TRepresentationType matchedRepresentation)
			where TRepresentationType : class
		{
			matchedRepresentation = null;

			if (representedType.ResolvedType != typeof (TResolvedType))
				return false;

			matchedRepresentation = representations.OfType<TRepresentationType> ().FirstOrDefault ();
			return matchedRepresentation != null;
		}
	}
}