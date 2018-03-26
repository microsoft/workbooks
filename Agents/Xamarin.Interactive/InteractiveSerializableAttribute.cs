// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive
{
    [AttributeUsage (
        AttributeTargets.Class |
        AttributeTargets.Struct |
        AttributeTargets.Enum)]
    class InteractiveSerializableAttribute : Attribute
    {
        public string TypeScriptTypeName { get; }
        public bool GenerateTypeScriptDefinition { get; }

        public InteractiveSerializableAttribute (
            string typeScriptTypeName,
            bool generateTypeScriptDefinition = true)
        {
            TypeScriptTypeName = typeScriptTypeName
                ?? throw new ArgumentNullException (nameof (typeScriptTypeName));
            GenerateTypeScriptDefinition = generateTypeScriptDefinition;
        }
    }
}