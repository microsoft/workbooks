//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// NodeType.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public enum NodeType : ushort
    {
        None = 0,
        Element = 1,
        [Obsolete]
        Attribute = 2,
        Text = 3,
        [Obsolete]
        CDataSection = 4,
        [Obsolete]
        EntityReference = 5,
        [Obsolete]
        Entity = 6,
        ProcessingInstruction = 7,
        Comment = 8,
        Document = 9,
        DocumentType = 10,
        DocumentFragment = 11,
        [Obsolete]
        Notation = 12
    }
}