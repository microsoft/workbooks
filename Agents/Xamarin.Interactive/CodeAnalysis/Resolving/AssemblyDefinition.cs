// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [JsonObject]
    public sealed class AssemblyDefinition
    {
        public AssemblyIdentity Name { get; }
        public AssemblyEntryPoint EntryPoint { get; }
        public AssemblyContent Content { get; }
        public IReadOnlyList<AssemblyDependency> ExternalDependencies { get; }
        public bool HasIntegration { get; }

        [JsonConstructor]
        AssemblyDefinition (
            AssemblyIdentity name,
            AssemblyEntryPoint entryPoint,
            AssemblyContent content,
            IReadOnlyList<AssemblyDependency> externalDependencies,
            bool hasIntegration)
        {
            Name = name;
            EntryPoint = entryPoint;
            Content = content;
            ExternalDependencies = externalDependencies;
            HasIntegration = hasIntegration;
        }

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
            : this (
                name,
                new AssemblyEntryPoint (entryPointType, entryPointMethod),
                new AssemblyContent (location, peImage, debugSymbols),
                externalDependencies,
                hasIntegration)
        {
        }
    }
}