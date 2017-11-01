//
// EventListener.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;

using Foundation;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
	[Protocol]
	public interface IEventListener : IJSExport
	{
		[Export ("handleEvent")]
		void HandleEvent ();
	}

	public class EventListener : NSObject, IEventListener
	{
		readonly Action<Event> handler;

		public EventListener (Action<Event> handler)
		{
			this.handler = handler;
		}

		void IEventListener.HandleEvent ()
		{
			handler?.Invoke (WrappedObject.Wrap<Event> (JSContext.CurrentArguments [0]));
		}
	}

	public class EventListener<TEvent> : EventListener where TEvent : Event
	{
		public EventListener (Action<TEvent> handler) : base (e => handler ((TEvent)e))
		{
		}
	}
}