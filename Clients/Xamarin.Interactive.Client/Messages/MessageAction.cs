//
// MessageAction.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Messages
{
	sealed class MessageAction
	{
		public const string DismissActionId = "_xi-message-action-dismiss";
		public const string RetryActionId = "_xi-message-action-retry";

		public Message Message { get; }
		public MessageActionKind Kind { get; }
		public string Id { get; }
		public string Label { get; }
		public string Tooltip { get; }

		MessageAction (
			Message message,
			MessageActionKind kind,
			string id,
			string label,
			string tooltip)
		{
			switch (kind) {
			case MessageActionKind.Affirmative:
			case MessageActionKind.Negative:
			case MessageActionKind.Auxiliary:
				break;
			default:
				throw new ArgumentOutOfRangeException (nameof (kind), $"{kind}");
			}

			if (label == null)
				throw new ArgumentNullException (nameof (label));

			Message = message;
			Kind = kind;
			Id = id;
			Label = label;
			Tooltip = tooltip;
		}

		public MessageAction (
			MessageActionKind kind,
			string id,
			string label,
			string tooltip = null)
			: this (null, kind, id, label, tooltip)
		{
		}

		public MessageAction WithMessage (Message message)
		{
			if (Message == message)
				return this;

			return new MessageAction (
				message,
				Kind,
				Id,
				Label,
				Tooltip);
		}
	}
}