//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Core
{
    [JsonObject]
    sealed class AgentFeatures : IEquatable<AgentFeatures>
    {
        public IReadOnlyList<string> SupportedViewInspectionHierarchies { get; }

        [JsonConstructor]
        public AgentFeatures (IReadOnlyList<string> supportedViewInspectionHierarchies)
            => SupportedViewInspectionHierarchies = supportedViewInspectionHierarchies;

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