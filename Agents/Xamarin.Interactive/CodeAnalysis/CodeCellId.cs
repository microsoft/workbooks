//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
    /// <summary>
    /// Essentially a mirror of a Roslyn DocumentId.
    /// </summary>
    public struct CodeCellId : IEquatable<CodeCellId>
    {
        internal static readonly CodeCellId Empty;

        public static CodeCellId Parse (string id)
        {
            if (string.IsNullOrEmpty (id))
                return Empty;

            var parts = id.Split ('/');
            return new CodeCellId (Guid.Parse (parts [0]), Guid.Parse (parts [1]));
        }

        public Guid ProjectId { get; }
        public Guid Id { get; }

        public CodeCellId (Guid projectId, Guid id)
        {
            ProjectId = projectId;
            Id = id;
        }

        // TODO: This doesn't use == comparison to workaround a bug in the Mono
        // interpreter (https://github.com/mono/mono/issues/8494). Revert this
        // to its old form once we bump the WASM SDK to include that fix. - brajkovic
        public bool Equals (CodeCellId id)
            => id.ProjectId.Equals (ProjectId) && id.Id.Equals (Id);

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

        public static implicit operator CodeCellId (string id)
            => Parse (id);

        public static implicit operator string (CodeCellId id)
            => id.ToString ();
    }
}