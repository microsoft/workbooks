// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    [AttributeUsage (
        AttributeTargets.Class |
        AttributeTargets.Struct |
        AttributeTargets.Enum)]
    sealed class MonacoSerializableAttribute : InteractiveSerializableAttribute
    {
        public MonacoSerializableAttribute (string typeScriptTypeName)
            : base (typeScriptTypeName, generateTypeScriptDefinition: false)
        {
        }
    }
}