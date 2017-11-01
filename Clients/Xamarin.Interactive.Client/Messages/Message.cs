//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Immutable;
using System.Threading;

namespace Xamarin.Interactive.Messages
{
    sealed class Message : IDisposable
    {
        static int lastId;

        readonly int id;
        public int Id => id;

        public IMessageService MessageService { get; }

        public MessageKind Kind { get; }
        public MessageSeverity Severity { get; }

        public string Text { get; }
        public string DetailedText { get; }
        public bool ShowSpinner { get; }

        public ImmutableArray<MessageAction> Actions { get; }
        public Action<Message, MessageAction> ActionResponseHandler { get; }

        public bool HasActions => Actions.Length > 0;

        public MessageAction AffirmativeAction
            => Actions.FirstOrDefault (a => a.Kind == MessageActionKind.Affirmative);

        public MessageAction NegativeAction
            => Actions.FirstOrDefault (a => a.Kind == MessageActionKind.Negative);

        public MessageAction AuxiliaryAction
            => Actions.FirstOrDefault (a => a.Kind == MessageActionKind.Auxiliary);

        Message (
            int id,
            IMessageService messageService,
            MessageKind kind,
            MessageSeverity severity,
            string text,
            string detailedText,
            bool showSpinner,
            ImmutableArray<MessageAction> actions,
            Action<Message, MessageAction> actionResponseHandler)
        {
            this.id = id;
            MessageService = messageService;
            Kind = kind;
            Severity = severity;
            Text = text;
            DetailedText = detailedText;
            ShowSpinner = showSpinner;
            Actions = actions;
            ActionResponseHandler = actionResponseHandler;
        }

        public void Dispose ()
            => MessageService?.DismissMessage (Id);

        public Message WithMessageService (IMessageService messageService)
            => new Message (
                id,
                messageService,
                Kind,
                Severity,
                Text,
                DetailedText,
                ShowSpinner,
                Actions,
                ActionResponseHandler);

        public Message WithActionResponseHandler (Action<Message, MessageAction> actionResponseHandler)
            => new Message (
                id,
                MessageService,
                Kind,
                Severity,
                Text,
                DetailedText,
                ShowSpinner,
                Actions,
                actionResponseHandler);

        public Message WithAction (MessageAction action)
        {
            if (action == null)
                throw new ArgumentNullException (nameof (action));

            return new Message (
                id,
                MessageService,
                Kind,
                Severity,
                Text,
                DetailedText,
                ShowSpinner,
                Actions.Add (action),
                ActionResponseHandler);
        }

        public static Message Create (
            MessageKind kind,
            MessageSeverity severity,
            string text,
            string detailedText = null,
            bool showSpinner = false)
            => new Message (
                Interlocked.Increment (ref lastId),
                null,
                kind,
                severity,
                text,
                detailedText,
                showSpinner,
                ImmutableArray<MessageAction>.Empty,
                null);

        public static Message CreateInfoStatus (
            string text,
            string detailedText = null,
            bool showSpinner = false)
            => Create (
                MessageKind.Status,
                MessageSeverity.Info,
                text,
                detailedText,
                showSpinner);

        public static Message CreateErrorStatus (string text, string detailedText = null)
            => Create (
                MessageKind.Status,
                MessageSeverity.Error,
                text,
                detailedText);

        public static Message CreateErrorAlert (string text, string detailedText = null)
            => Create (
                MessageKind.Alert,
                MessageSeverity.Error,
                text,
                detailedText);


        public static Message CreateErrorAlert (UserPresentableException exception)
            => Create (
                MessageKind.Alert,
                MessageSeverity.Error,
                exception.Message,
                exception.Details);

        public static Message CreateInfoAlert (string text, string detailedText = null)
            => Create (
                MessageKind.Alert,
                MessageSeverity.Info,
                text,
                detailedText);
    }
}