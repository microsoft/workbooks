// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    /// <summary>
    /// An opaque identifier for <see cref="EvaluationContext"/>.
    /// </summary>
    public struct EvaluationContextId : IEquatable<EvaluationContextId>
    {
        internal static EvaluationContextId Create ()
            => new EvaluationContextId (Guid.NewGuid ());

        readonly Guid id;

        EvaluationContextId (Guid id)
            => this.id = id;

        // TODO: This doesn't use == comparison to workaround a bug in the Mono
        // interpreter (https://github.com/mono/mono/issues/8494). Revert this
        // to its old form once we bump the WASM SDK to include that fix. - brajkovic
        public bool Equals (EvaluationContextId id)
            => this.id.Equals (id.id);

        public override bool Equals (object obj)
            => obj is EvaluationContextId id && Equals (id);

        public override int GetHashCode ()
            => id.GetHashCode ();

        public override string ToString ()
            => id.ToString ();

        public static bool operator == (EvaluationContextId a, EvaluationContextId b)
            => a.Equals (b);

        public static bool operator != (EvaluationContextId a, EvaluationContextId b)
            => !a.Equals (b);

        public static implicit operator string (EvaluationContextId id)
            => id.ToString ();

        public static explicit operator EvaluationContextId (string id)
            => new EvaluationContextId (Guid.Parse (id));
    }
}