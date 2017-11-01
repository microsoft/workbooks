//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// CssStyleSheet.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public partial class CssStyleSheet : StyleSheet
    {
        internal CssStyleSheet (JSValue underlyingJSValue) : base (underlyingJSValue)
        {
        }

        public void InsertRule (string rule, int index)
        {
            UnderlyingJSValue.Invoke ("insertRule", JSValue.From (rule, UnderlyingJSValue.Context), JSValue.From (index, UnderlyingJSValue.Context));
        }

        public void DeleteRule (int index)
        {
            UnderlyingJSValue.Invoke ("deleteRule", JSValue.From (index, UnderlyingJSValue.Context));
        }
    }
}