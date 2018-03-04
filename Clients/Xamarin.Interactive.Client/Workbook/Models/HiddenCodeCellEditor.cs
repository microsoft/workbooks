//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;

namespace Xamarin.Interactive.Workbook.Models
{
    sealed class HiddenCodeCellEditor : IEditor
    {
        public IObservable<EditorEvent> Events => throw new NotImplementedException ();

        public bool IsDisposed { get; private set; }

        public void Dispose ()
            => IsDisposed = true;

        public void ExecuteCommand (EditorCommand command)
        {
            throw new NotImplementedException ();
        }

        public void Focus ()
        {
            throw new NotImplementedException ();
        }

        public IEnumerable<EditorCommand> GetCommands ()
        {
            throw new NotImplementedException ();
        }

        public EditorCommandStatus GetCommandStatus (EditorCommand command)
        {
            throw new NotImplementedException ();
        }

        public void OnBlur ()
        {
            throw new NotImplementedException ();
        }

        public void SetCursorPosition (AbstractCursorPosition cursorPosition)
        {
            throw new NotImplementedException ();
        }

        public bool TryGetCommand (string commandId, out EditorCommand command)
        {
            throw new NotImplementedException ();
        }
    }
}