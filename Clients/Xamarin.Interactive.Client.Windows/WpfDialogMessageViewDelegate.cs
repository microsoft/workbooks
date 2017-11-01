// WpfDialogMessageViewDelegate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Windows;

using MahApps.Metro.Controls.Dialogs;

using Xamarin.Interactive.Client.Windows.Views;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.Client.Windows
{
	sealed class WpfDialogMessageViewDelegate : IAlertMessageViewDelegate
	{
		readonly Window window;
		readonly Dictionary<int, MetroDialogWindow> dialogs = new Dictionary<int, MetroDialogWindow> ();

		public WpfDialogMessageViewDelegate (Window window)
		{
			if (window == null)
				throw new ArgumentNullException (nameof (window));

			this.window = window;
		}

		void IAlertMessageViewDelegate.DisplayMessage (Message message)
		{
			var affirmativeAction = message.AffirmativeAction;
			var negativeAction = message.NegativeAction;
			var auxiliaryAction = message.AuxiliaryAction;

			if (affirmativeAction == null)
				return;

			var dialog = new MetroDialogWindow {
				Owner = window,
				Width = window.Width,
				ButtonStyle = MessageDialogStyle.Affirmative,
				Title = message.Text,
				Message = message.DetailedText ?? message.Text
			};

			dialog.AffirmativeButtonText = affirmativeAction.Label;

			if (negativeAction != null) {
				dialog.ButtonStyle = MessageDialogStyle.AffirmativeAndNegative;
				dialog.NegativeButtonText = negativeAction.Label;
				if (auxiliaryAction != null) {
					dialog.ButtonStyle = MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary;
					dialog.FirstAuxiliaryButtonText = auxiliaryAction.Label;
				}
			}

			dialogs.Add (message.Id, dialog);

			dialog.Closed += (sender, e) => {
				MessageAction action = null;
				switch (dialog.Result) {
				case MessageDialogResult.Affirmative:
					action = affirmativeAction;
					break;
				case MessageDialogResult.Negative:
					action = negativeAction;
					break;
				case MessageDialogResult.FirstAuxiliary:
					action = auxiliaryAction;
					break;
				}

				if (action != null && dialogs.ContainsKey (message.Id))
					message?.ActionResponseHandler (message, action);
			};

			dialog.ShowDialog ();
		}

		void IAlertMessageViewDelegate.DismissMessage (int messageId)
		{
			MetroDialogWindow dialog;
			if (dialogs.TryGetValue (messageId, out dialog)) {
				dialogs.Remove (messageId);
				dialog.Close ();
			}
		}
	}
}