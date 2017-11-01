//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// Event.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
	public partial class Event : WrappedObject
	{
		internal Event (ScriptContext context, IDOMEvent comObject) : base (context, comObject)
		{
		}

		public bool Bubbles {
			get {
				return ((IDOMEvent)ComObject).bubbles;
			}
		}

		public bool Cancelable {
			get {
				return ((IDOMEvent)ComObject).cancelable;
			}
		}

		public EventTarget CurrentTarget {
			get {
				return Wrap<EventTarget> (((IDOMEvent)ComObject).currentTarget);
			}
		}

		public bool DefaultPrevented {
			get {
				return ((IDOMEvent)ComObject).defaultPrevented;
			}
		}

		public EventPhase EventPhase {
			get {
				return (EventPhase)((IDOMEvent)ComObject).eventPhase;
			}
		}

		public EventTarget Target {
			get {
				return Wrap<EventTarget> (((IDOMEvent)ComObject).target);
			}
		}

		public string Type {
			get {
				return ((IDOMEvent)ComObject).type;
			}
		}

		public bool IsTrusted {
			get {
				return ((IDOMEvent)ComObject).isTrusted;
			}
		}

		public void PreventDefault ()
		{
			((IDOMEvent)ComObject).preventDefault ();
		}

		public void StopImmediatePropagation ()
		{
			((IDOMEvent)ComObject).stopImmediatePropagation ();
		}

		public void StopPropagation ()
		{
			((IDOMEvent)ComObject).stopPropagation ();
		}
	}
}