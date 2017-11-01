//
// MessageViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Messages
{
	sealed class MessageViewController : IMessageService
	{
		ImmutableArray<IMessageService> routes = ImmutableArray<IMessageService>.Empty;

		public MessageViewController (
			IStatusMessageViewDelegate statusMessageViewDelegate,
			IAlertMessageViewDelegate alertMessageViewDelegate)
		{
			if (statusMessageViewDelegate != null)
				AddRoute (new StatusMessageViewController (statusMessageViewDelegate));

			if (alertMessageViewDelegate != null)
				AddRoute (new AlertMessageViewController (alertMessageViewDelegate));
		}

		void AddRoute (IMessageService messageService)
		{
			if (messageService == null)
				throw new ArgumentNullException (nameof (messageService));

			routes = routes.Add (messageService);
		}

		public void ClearStatusMessages ()
			=> routes.OfType<StatusMessageViewController> ().FirstOrDefault ()?.DismissAll ();

		public Message PushMessage (Message message)
		{
			if (message == null)
				throw new ArgumentNullException (nameof (message));

			if (message.Kind == MessageKind.Alert) {
				if (!message.HasActions)
					message = message.WithAction (
						new MessageAction (
							MessageActionKind.Affirmative,
							MessageAction.DismissActionId,
							Catalog.GetString ("OK")));

				if (message.ActionResponseHandler == null)
					message = message.WithActionResponseHandler ((msg, _) => msg.Dispose ());
			}

			LogMessage (message);

			foreach (var route in routes) {
				if (route.CanHandleMessage (message))
					return route.PushMessage (message);
			}

			throw new NotImplementedException (
				$"support for {nameof (MessageKind)}.{message.Kind} not implemented");
		}

		public Task<MessageAction> PushAlertMessageAsync (
			Message message,
			bool disposeMessageOnResponse = true)
		{
			if (message == null)
				throw new ArgumentNullException (nameof (message));

			if (message.Kind != MessageKind.Alert)
				throw new ArgumentException (
					"message.Kind must be MessageKind.Alert", nameof (message));

			if (message.ActionResponseHandler != null)
				throw new ArgumentException (
					"message.ActionResponseHandler must be null", nameof (message));

			var taskCompletionSource = new TaskCompletionSource<MessageAction> ();

			PushMessage (message.WithActionResponseHandler ((m, a) => {
				if (disposeMessageOnResponse)
					m.Dispose ();

				taskCompletionSource.SetResult (a.WithMessage (m));
			}));

			return taskCompletionSource.Task;
		}

		bool IMessageService.CanHandleMessage (Message message)
		{
			throw new NotSupportedException ();
		}

		void IMessageService.DismissMessage (int messageId)
		{
			throw new NotSupportedException ();
		}

		void LogMessage (Message message)
		{
			LogLevel logLevel;
			switch (message.Severity) {
			case MessageSeverity.Error:
				logLevel = LogLevel.Error;
				break;
			default:
				logLevel = LogLevel.Info;
				break;
			}

			var logText = $"{nameof (PushMessage)} => {message.Kind}: {message.Text}";
			if (!String.IsNullOrEmpty (message.DetailedText))
				logText += $" ({message.DetailedText})";

			Log.Commit (logLevel, nameof (MessageViewController), logText);
		}
	}
}