//
// UserPresentableExceptionHandler.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Mac
{
	static class UserPresentableExceptionHandler
	{
		const string TAG = nameof (UserPresentableExceptionHandler);

		public static void Present (this Exception e, object uiContext = null)
		{
			var aggregate = e as AggregateException;
			if (aggregate != null)
				e = aggregate.Flatten ().InnerException;

			var userPresentable = e as UserPresentableException;
			if (userPresentable == null)
				userPresentable = e.ToUserPresentable (Catalog.GetString ("Unhandled Exception"));

			userPresentable.Present (uiContext);
		}

		public static void Present (this UserPresentableException e, object uiContext = null)
		{
			Log.Error (TAG, $"{e.Message} ({e.Details})", e.InnerException ?? e);

			new Telemetry.Events.Error (e).Post ();

			MainThread.Ensure ();

			uiContext = uiContext ?? e.UIContext;

			var window = (uiContext as NSViewController)?.View?.Window
				?? (uiContext as NSView)?.Window
				?? (uiContext as NSWindowController)?.Window
				?? uiContext as NSWindow;

			var alert = new NSAlert {
				AlertStyle = NSAlertStyle.Critical,
				MessageText = e.Message,
				InformativeText = e.Details
			};

			if (window == null)
				alert.RunModal ();
			else
				alert.BeginSheet (window, resp => { });
		}
	}
}