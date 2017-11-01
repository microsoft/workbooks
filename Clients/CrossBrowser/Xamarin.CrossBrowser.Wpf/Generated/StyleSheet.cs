//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// StyleSheet.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class StyleSheet : WrappedObject
    {
        internal StyleSheet (ScriptContext context, IHTMLStyleSheet comObject) : base (context, (Object)comObject)
        {
        }

        public string Href {
            get {
                return ((IHTMLStyleSheet)ComObject).href;
            }
        }

        public string Id {
            get {
                return ((IHTMLStyleSheet)ComObject).id;
            }
        }

        public string Title {
            get {
                return ((IHTMLStyleSheet4)ComObject).title;
            }
        }

        public string Type {
            get {
                return ((IHTMLStyleSheet4)ComObject).type;
            }
        }

        public Node OwnerNode {
            get {
                return Wrap<Node> (((IHTMLStyleSheet4)ComObject).ownerNode);
            }
        }
    }
}