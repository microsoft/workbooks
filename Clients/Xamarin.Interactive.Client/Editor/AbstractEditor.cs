//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Editor.Events;

namespace Xamarin.Interactive.Editor
{
    abstract class AbstractEditor : IEditor
    {
        readonly Observable<EditorEvent> events = new Observable<EditorEvent> ();
        public IObservable<EditorEvent> Events => events;
        protected IObserver<EditorEvent> EventsObserver => events.Observers;
        public bool IsDisposed { get; private set; }

        public abstract void Focus ();

        public virtual void OnBlur () { }

        public abstract void SetCursorPosition (AbstractCursorPosition cursorPosition);

        public virtual bool TryGetCommand (string commandId, out EditorCommand command)
        {
            command = default (EditorCommand);
            return false;
        }

        public virtual IEnumerable<EditorCommand> GetCommands ()
        {
            yield break;
        }

        public virtual void ExecuteCommand (EditorCommand command)
        {
            throw new NotImplementedException ();
        }

        public virtual EditorCommandStatus GetCommandStatus (EditorCommand command) =>
            EditorCommandStatus.Hidden;

        public void Dispose ()
        {
            if (IsDisposed)
                return;

            Dispose (true);
            IsDisposed = true;
        }

        protected virtual void Dispose (bool disposing)
        {
        }
    }
}