//
// CodeCellId.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
	/// <summary>
	/// Essentially a mirror of a Roslyn DocumentId.
	/// </summary>
	[Serializable]
	public struct CodeCellId : IEquatable<CodeCellId>
	{
		internal Guid ProjectId { get; }
		internal Guid Id { get; }

		internal CodeCellId (Guid projectId, Guid id)
		{
			ProjectId = projectId;
			Id = id;
		}

		public bool Equals (CodeCellId id)
			=> id.ProjectId == ProjectId && id.Id == Id;

		public override bool Equals (object obj)
			=> obj is CodeCellId id && Equals (id);

		public override int GetHashCode ()
			=> Hash.Combine (ProjectId.GetHashCode (), Id.GetHashCode ());

		public override string ToString ()
			=> $"{ProjectId}/{Id}";

		public static bool operator == (CodeCellId a, CodeCellId b)
			=> a.Equals (b);

		public static bool operator != (CodeCellId a, CodeCellId b)
			=> !a.Equals (b);
	}
}