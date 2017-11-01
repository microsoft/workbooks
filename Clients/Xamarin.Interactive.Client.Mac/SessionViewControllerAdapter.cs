//
// SessionViewControllerAdapter.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using AppKit;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Mac
{
	sealed class SessionViewControllerAdapter<TViewController> : IObserver<ClientSessionEvent>
		where TViewController : NSViewController, IObserver<ClientSessionEvent>
	{
		const string TAG = nameof (SessionViewControllerAdapter<TViewController>);

		readonly TViewController viewController;

		public SessionViewControllerAdapter (TViewController viewController)
		{
			if (viewController == null)
				throw new ArgumentNullException (nameof (viewController));

			this.viewController = viewController;
		}

		ClientSession session;
		public ClientSession Session {
			get {
				if (session != null)
					return session;
				
				var windowController = viewController.View?.Window?.WindowController;
				if (windowController == null)
					return null;

				var sessionWindowController = windowController as SessionWindowController;
				if (sessionWindowController == null) {
					Log.Critical (
						TAG,
						"Should never happen: window controller is non null but" +
						$"not an instance of {nameof (SessionWindowController)}.");

					throw new InvalidCastException (nameof (SessionWindowController));
				}

				session = sessionWindowController.Session;

				return session;
			}
		}

		bool sessionAvailable;

		public void ViewDidAppear ()
		{
			if (sessionAvailable)
				return;

			var session = Session;

			if (session != null) {
				sessionAvailable = true;
				session.Subscribe (this);
			}
		}

		public void ValidateUserInterface ()
			=> viewController.View?.Window?.Toolbar?.ValidateVisibleItems ();

		void IObserver<ClientSessionEvent>.OnNext (ClientSessionEvent evnt)
		{
			viewController.OnNext (evnt);
			ValidateUserInterface ();
		}

		void IObserver<ClientSessionEvent>.OnError (Exception error)
		{
			viewController.OnError (error);
			ValidateUserInterface ();
		}

		void IObserver<ClientSessionEvent>.OnCompleted ()
		{
			viewController.OnCompleted ();
			ValidateUserInterface ();
		}
	}
}