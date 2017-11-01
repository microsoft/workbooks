//
// IAssemblyEntryPoint.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace Xamarin.Interactive.CodeAnalysis
{
    public interface IAssemblyEntryPoint
    {
        string TypeName { get; }
        string MethodName { get; }
    }
}