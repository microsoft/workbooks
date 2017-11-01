//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class AssemblyEntryPoint : IAssemblyEntryPoint
    {
        public string TypeName { get; }
        public string MethodName { get; }

        public AssemblyEntryPoint (string typeName, string methodName)
        {
            TypeName = typeName;
            MethodName = methodName;
        }
    }
}