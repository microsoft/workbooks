//
// XIEditorMenuItem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.Editor;

namespace Xamarin.Interactive.Client.Mac.Menu
{
	[Register ("XIEditorMenuItem")]
	sealed class XIEditorMenuItem : NSMenuItem
	{
		string commandId;
		IEditor currentEditor;
		EditorCommand command;
		EditorCommandStatus status;

		public XIEditorMenuItem (NSCoder coder) : base (coder)
		{
		}

		public XIEditorMenuItem (IntPtr handle) : base (handle)
		{
		}

		public override void AwakeFromNib ()
		{
			commandId = Title;
			Activated += (sender, e) => currentEditor?.ExecuteCommand (command);
		}

		public void Bind (IEditor editor)
		{
			currentEditor = editor;

			if (editor == null || !editor.TryGetCommand (commandId, out command)) {
				command = default (EditorCommand);
				status = EditorCommandStatus.Unsupported;
			} else {
				Title = command.Title;
				status = editor.GetCommandStatus (command);
			}

			Hidden = status == EditorCommandStatus.Hidden || status == EditorCommandStatus.Unsupported;
		}

		public override bool Enabled {
			get { return status == EditorCommandStatus.Enabled; }
			set { }
		}
	}
}