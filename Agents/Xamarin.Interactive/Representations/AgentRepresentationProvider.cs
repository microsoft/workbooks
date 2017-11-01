//
// AgentRepresentationProvider.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    abstract class AgentRepresentationProvider : TypeMapRepresentationProvider
    {
        /// <summary>
		/// Normalizers can only be implemented by core agents (cannot be registered
		/// via public API) and are "trusted" in that they follow the conventions of
		/// <see cref="RepresentationManager.Normalize"/>. Returning non-null from
		/// this method will seal the fate of the object - it will be returned directly
		/// for serialization and will not be further normalized.
		/// </summary>
        public virtual object NormalizeRepresentation (object obj)
        {
            return null;
        }
    }
}