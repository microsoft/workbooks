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
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public partial class StyleSheetList : WrappedObject, IReadOnlyList<CssStyleSheet>
    {
        internal StyleSheetList (JSValue underlyingJSValue) : base (underlyingJSValue)
        {
        }

        public int Count {
            get {
                return UnderlyingJSValue.GetProperty ("length").ToInt32 ();
            }
        }

        public CssStyleSheet this [int index] {
            get {
                return Wrap<CssStyleSheet> (UnderlyingJSValue.Invoke ("item", JSValue.From (index, UnderlyingJSValue.Context)));
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