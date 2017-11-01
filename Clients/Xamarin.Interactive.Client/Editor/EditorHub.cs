//
// EditorHub.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Editor.Events;

namespace Xamarin.Interactive.Editor
{
	class EditorHub<TEditorState> : IEditor
	{
		readonly Observable<EditorEvent> events = new Observable<EditorEvent> ();
		public bool IsDisposed => false;

		struct EventSubscription : IDisposable
		{
			readonly IDisposable [] subscriptions;

			public EventSubscription (params IDisposable [] subscriptions)
			{
				this.subscriptions = subscriptions;
			}

			public void Dispose () => subscriptions.ForEach (subscription => subscription.Dispose ());
		}

		ImmutableArray<IEditor> editors = ImmutableArray<IEditor>.Empty;
		ImmutableDictionary<IEditor, TEditorState> editorStateMap
			= ImmutableDictionary<IEditor, TEditorState>.Empty;
		ImmutableDictionary<IEditor, IDisposable> editorEventSubscriptions
			= ImmutableDictionary<IEditor, IDisposable>.Empty;

		public IEditor FocusedEditor { get; private set; }

		public TEditorState FocusedEditorState {
			get {
				var editor = FocusedEditor;
				if (editor == null)
					return default (TEditorState);
				TEditorState editorState;
				editorStateMap.TryGetValue (editor, out editorState);
				return editorState;
			}
		}

		public IObservable<EditorEvent> Events => events;

		public void AddEditor (IEditor editor, TEditorState editorState)
		{
			if (editors.Contains (editor))
				throw new ArgumentException ("instance is already added", nameof (editor));

			editorEventSubscriptions = editorEventSubscriptions.Add (
				editor,
				new EventSubscription (
					editor.Events.Subscribe (new Observer<EditorEvent> (OnNext)),
					editor.Events.Subscribe (events.Observers)
				));

			editors = editors.Add (editor);
			editorStateMap = editorStateMap.Add (editor, editorState);
		}

		public void RemoveEditor (IEditor editor)
		{
			var index = editors.IndexOf (editor);
			if (index < 0)
				throw new ArgumentException ("instance is not added", nameof (editor));

			editors = editors.RemoveAt (index);
			editorStateMap = editorStateMap.Remove (editor);

			IDisposable subscription;
			if (editorEventSubscriptions.TryGetValue (editor, out subscription)) {
				subscription.Dispose ();
				editorEventSubscriptions = editorEventSubscriptions.Remove (editor);
			}

			if (FocusedEditor == editor)
				FocusedEditor = null;
		}

		void OnNext (EditorEvent evnt)
		{
			if (evnt is FocusEvent)
				FocusedEditor = evnt.Source;
		}

		public void Focus () => FocusedEditor?.Focus ();

		public void OnBlur () => FocusedEditor?.OnBlur ();

		public void SetCursorPosition (AbstractCursorPosition cursorPosition)
			=> FocusedEditor?.SetCursorPosition (cursorPosition);

		public IEnumerable<EditorCommand> GetCommands ()
			=> FocusedEditor?.GetCommands () ?? EmptyArray<EditorCommand>.Instance;

		public bool TryGetCommand (string commandId, out EditorCommand command)
		{
			var editor = FocusedEditor;
			if (editor == null) {
				command = default (EditorCommand);
				return false;
			}

			return editor.TryGetCommand (commandId, out command);
		}

		public void ExecuteCommand (EditorCommand command)
			=> FocusedEditor?.ExecuteCommand (command);

		public EditorCommandStatus GetCommandStatus (EditorCommand command)
			=> FocusedEditor?.GetCommandStatus (command) ?? EditorCommandStatus.Unsupported;

		public void Dispose () { }
	}
}