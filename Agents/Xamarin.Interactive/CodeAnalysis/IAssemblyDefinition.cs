//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis
{
    /// <summary>
    /// Represents a complete assembly as loaded in the agent.
    /// </summary>
    public interface IAssemblyDefinition
    {
        IAssemblyIdentity Identity { get; }
        IAssemblyContent Content { get; }
        IAssemblyEntryPoint EntryPoint { get; }
    }
}