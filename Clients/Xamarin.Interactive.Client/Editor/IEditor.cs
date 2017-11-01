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
    interface IEditor : IDisposable
    {
        IObservable<EditorEvent> Events { get; }
        bool IsDisposed { get; }

        void Focus ();
        void OnBlur ();
        void SetCursorPosition (AbstractCursorPosition cursorPosition);

        IEnumerable<EditorCommand> GetCommands ();
        bool TryGetCommand (string commandId, out EditorCommand command);
        EditorCommandStatus GetCommandStatus (EditorCommand command);
        void ExecuteCommand (EditorCommand command);
    }
}