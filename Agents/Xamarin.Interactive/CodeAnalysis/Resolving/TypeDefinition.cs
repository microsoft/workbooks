// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [JsonObject]
    public sealed class TypeDefinition
    {
        public AssemblyDefinition Assembly { get; }
        public string Name { get; }

        [NonSerialized]
        readonly Type resolvedType;
        public Type ResolvedType => resolvedType;

        [JsonConstructor]
        public TypeDefinition (
            AssemblyDefinition assembly,
            string name,
            Type resolvedType = null)
        {
            Assembly = assembly;
            Name = name;
            this.resolvedType = resolvedType;
        }

        public TypeDefinition WithResolvedType (Type resolvedType)
            => new TypeDefinition (Assembly, Name, resolvedType);
    }
}