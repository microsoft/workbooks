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
using mshtml;

namespace Xamarin.CrossBrowser
{
	public partial class EventTarget : WrappedObject
	{
		internal EventTarget (ScriptContext context, IEventTarget comObject) : base (context, (Object)comObject)
		{
		}

		public void AddEventListener (string type, EventListener listener, bool useCapture = false)
		{
			((IEventTarget)ComObject).addEventListener (type, listener, useCapture);
		}

		public void RemoveEventListener (string type, EventListener listener, bool useCapture = false)
		{
			((IEventTarget)ComObject).removeEventListener (type, listener, useCapture);
		}

		public bool DispatchEvent (Event @event)
		{
			return ((IEventTarget)ComObject).dispatchEvent ((IDOMEvent)@event.ComObject);
		}
	}
}