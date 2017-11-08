//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;

namespace Xamarin.Interactive.Client.Console
{
    sealed class ConsoleEditor : IEditor
    {
        public IObservable<EditorEvent> Events { get; } = new Observable<EditorEvent> ();

        public bool IsDisposed { get; private set; }

        public void Dispose () => IsDisposed = true;

        public void Focus ()
        {
        }

        public void OnBlur ()
        {
        }

        public void SetCursorPosition (AbstractCursorPosition cursorPosition)
        {
        }

        public IEnumerable<EditorCommand> GetCommands ()
            => Array.Empty<EditorCommand> ();

        public EditorCommandStatus GetCommandStatus (EditorCommand command)
            => EditorCommandStatus.Unsupported;

        public bool TryGetCommand (string commandId, out EditorCommand command)
        {
            command = default (EditorCommand);
            return false;
        }

        public void ExecuteCommand (EditorCommand command)
        {
        }
    }
}