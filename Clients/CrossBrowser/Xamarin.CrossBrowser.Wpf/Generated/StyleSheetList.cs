//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// StyleSheetList.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class StyleSheetList : WrappedObject, IReadOnlyList<CssStyleSheet>
    {
        internal StyleSheetList (ScriptContext context, IHTMLStyleSheetsCollection comObject) : base (context, (Object)comObject)
        {
        }

        public int Count {
            get {
                return ((IHTMLStyleSheetsCollection)ComObject).length;
            }
        }

        public CssStyleSheet this [int index] {
            get {
                return Wrap<CssStyleSheet> (((IHTMLStyleSheetsCollection2)ComObject).item (index));
            }
        }

        public IEnumerator<CssStyleSheet> GetEnumerator ()
        {
            for (int i = 0, n = Count; i < n; i++)
                yield return this [i];
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
    }
}