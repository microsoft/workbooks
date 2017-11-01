//
// EvaluationContextId.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Globalization;

namespace Xamarin.Interactive.CodeAnalysis
{
	/// <summary>
	/// An opaque identifier for <see cref="IEvaluationContext"/>.
	/// </summary>
	public struct EvaluationContextId : IEquatable<EvaluationContextId>
	{
		readonly int id;

		internal EvaluationContextId (int id)
			=> this.id = id;

		public bool Equals (EvaluationContextId id)
			=> id.id == this.id;

		public override bool Equals (object obj)
			=> obj is EvaluationContextId id && Equals (id);

		public override int GetHashCode ()
			=> id;

		public override string ToString ()
			=> id.ToString (CultureInfo.InvariantCulture);

		public static bool operator == (EvaluationContextId a, EvaluationContextId b)
			=> a.Equals (b);

		public static bool operator != (EvaluationContextId a, EvaluationContextId b)
			=> !a.Equals (b);

		public static implicit operator EvaluationContextId (int id)
			=> new EvaluationContextId (id);
	}
}