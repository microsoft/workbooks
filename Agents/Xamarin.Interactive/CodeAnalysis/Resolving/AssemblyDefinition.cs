// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [Serializable]
    public sealed class AssemblyDefinition
    {
        public AssemblyIdentity Name { get; }
        public AssemblyEntryPoint EntryPoint { get; }
        public AssemblyContent Content { get; }
        public AssemblyDependency [] ExternalDependencies { get; }
        public bool HasIntegration { get; }

        public AssemblyDefinition (
            AssemblyName name,
            FilePath location,
            string entryPointType = null,
            string entryPointMethod = null,
            byte [] peImage = null,
            byte [] debugSymbols = null,
            AssemblyDependency [] externalDependencies = null,
            bool hasIntegration = false)
            : this (
                new AssemblyIdentity (name),
                location,
                entryPointType,
                entryPointMethod,
                peImage,
                debugSymbols,
                externalDependencies,
                hasIntegration)
        {
        }

        public AssemblyDefinition (
            AssemblyIdentity name,
            FilePath location,
            string entryPointType = null,
            string entryPointMethod = null,
            byte [] peImage = null,
            byte [] debugSymbols = null,
            AssemblyDependency [] externalDependencies = null,
            bool hasIntegration = false)
        {
            Name = name;
            EntryPoint = new AssemblyEntryPoint (entryPointType, entryPointMethod);
            Content = new AssemblyContent (location, peImage, debugSymbols);
            ExternalDependencies = externalDependencies;
            HasIntegration = hasIntegration;
        }
    }
}