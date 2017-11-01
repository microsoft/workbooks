//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// EventTarget.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public partial class EventTarget : WrappedObject
    {
        internal EventTarget (JSValue underlyingJSValue) : base (underlyingJSValue)
        {
        }

        public void AddEventListener (string type, EventListener listener, bool useCapture = false)
        {
            UnderlyingJSValue.Invoke ("addEventListener", JSValue.From (type, UnderlyingJSValue.Context), JSValue.From (listener, UnderlyingJSValue.Context), JSValue.From (useCapture, UnderlyingJSValue.Context));
        }

        public void RemoveEventListener (string type, EventListener listener, bool useCapture = false)
        {
            UnderlyingJSValue.Invoke ("removeEventListener", JSValue.From (type, UnderlyingJSValue.Context), JSValue.From (listener, UnderlyingJSValue.Context), JSValue.From (useCapture, UnderlyingJSValue.Context));
        }

        public bool DispatchEvent (Event @event)
        {
            return UnderlyingJSValue.Invoke ("dispatchEvent", JSValue.From (@event.UnderlyingJSValue, UnderlyingJSValue.Context)).ToBool ();
        }
    }
}