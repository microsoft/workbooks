// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [JsonObject]
    public struct AssemblyEntryPoint
    {
        public string TypeName { get; }
        public string MethodName { get; }

        [JsonConstructor]
        internal AssemblyEntryPoint (string typeName, string methodName)
        {
            TypeName = typeName;
            MethodName = methodName;
        }
    }
}