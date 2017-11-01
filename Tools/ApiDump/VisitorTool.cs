//
// VisitorTool.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using ICSharpCode.NRefactory.CSharp;

namespace ApiDump
{
	public interface IVisitorTool
	{
		IAstVisitor Visitor { get; }
	}
}