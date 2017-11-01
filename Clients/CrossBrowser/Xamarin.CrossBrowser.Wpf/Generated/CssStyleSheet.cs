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
using mshtml;

namespace Xamarin.CrossBrowser
{
	public partial class CssStyleSheet : StyleSheet
	{
		internal CssStyleSheet (ScriptContext context, IHTMLStyleSheet comObject) : base (context, comObject)
		{
		}

		public void InsertRule (string rule, int index)
		{
			((IHTMLStyleSheet4)ComObject).insertRule (rule, index);
		}

		public void DeleteRule (int index)
		{
			((IHTMLStyleSheet4)ComObject).deleteRule (index);
		}
	}
}