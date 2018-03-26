// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [Serializable]
    public struct AssemblyEntryPoint
    {
        public string TypeName { get; }
        public string MethodName { get; }

        internal AssemblyEntryPoint (string typeName, string methodName)
        {
            TypeName = typeName;
            MethodName = methodName;
        }
    }
}