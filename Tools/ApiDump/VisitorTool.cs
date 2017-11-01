//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.NRefactory.CSharp;

namespace ApiDump
{
    public interface IVisitorTool
    {
        IAstVisitor Visitor { get; }
    }
}