//
// EventTarget.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.CrossBrowser
{
	public partial class EventTarget
	{
		public EventListener AddEventListener (string type, Action<Event> handler, bool useCapture = false)
		{
			#if MAC
			var listener = new EventListener (handler);
			#else
			var listener = new EventListener (Context, handler);
			#endif
			AddEventListener (type, listener, useCapture);
			return listener;
		}

		public EventListener<TEvent> AddEventListener<TEvent> (string type, Action<TEvent> handler,
			bool useCapture = false) where TEvent : Event
		{
			#if MAC
			var listener = new EventListener<TEvent> (handler);
			#else
			var listener = new EventListener<TEvent> (Context, handler);
			#endif
			AddEventListener (type, listener, useCapture);
			return listener;
		}
	}
}