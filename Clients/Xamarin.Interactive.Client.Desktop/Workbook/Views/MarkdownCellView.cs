//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Rendering;
using Xamarin.Interactive.Workbook.Events;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Views
{
    sealed class MarkdownCellView : CellView, IMarkdownCellView
    {
        readonly HtmlElement editorElem;
        readonly ProseMirrorEditor editor;

        public override IEditor Editor => editor;

        public MarkdownCellView (MarkdownCell markdownCell, HtmlDocument document)
            : base (document, "text")
        {
            var editorContainerElem = CreateContentContainer (null);
            ContentElement.AppendChild (editorContainerElem);

            editorElem = Document.CreateElement ("div");
            editorContainerElem.AppendChild (editorElem);

            editor = new ProseMirrorEditor (markdownCell, editorElem);
        }

        public string MarkdownContent {
            get { return editor.MarkdownContent; }
            set { editor.MarkdownContent = value; }
        }

        public override void Focus (bool scrollIntoView = true)
        {
            if (scrollIntoView)
                RootElement.ScrollIntoView ();
            editor.Focus ();
        }

        class ProseMirrorEditor : CellEditorView
        {
            #pragma warning disable 0414
            readonly dynamic proseMirror;
            #pragma warning restore 0414

            public HtmlElement ContentElement { get; }
            public sealed override Cell Cell { get; }

            public ProseMirrorEditor (MarkdownCell markdownCell, HtmlElement editorElem)
            {
                Cell = markdownCell
                    ?? throw new ArgumentNullException (nameof (markdownCell));

                ContentElement = editorElem
                    ?? throw new ArgumentNullException (nameof (editorElem));

                UpdateTheme ();

                var jsContext = editorElem.Context;
                var jsGlobal = jsContext.GlobalObject;

                proseMirror = jsGlobal.xiexports.WorkbookEditor (jsContext.CreateObject (o => {
                    o.placeElem = editorElem;
                    o.onFocus = (ScriptAction)HandleFocus;
                    o.onChange = (ScriptAction)HandleChange;
                    o.onCursorUpDown = (ScriptFunc)HandleCursorUpDown;
                    o.onModEnter = (ScriptAction)HandleModEnter;
                }));

                proseMirror.setMenuStyle ("tooltip");
            }

            protected override void Dispose (bool isDisposing)
            {
                proseMirror.dispose ();
                EventsObserver.OnNext (new DeleteCellEvent<MarkdownCell> ((MarkdownCell)Cell));
            }

            string currentThemeCssClass;

            void UpdateTheme ()
            {
                switch (Prefs.UI.Theme.ProseTypography.GetValue ()) {
                case "sans-serif":
                    currentThemeCssClass = "theme-prose-typography-sans-serif";
                    ContentElement.AddCssClass (currentThemeCssClass);
                    break;
                default:
                    if (currentThemeCssClass != null) {
                        ContentElement.RemoveCssClass (currentThemeCssClass);
                        currentThemeCssClass = null;
                    }
                    break;
                }
            }

            public override void SetCursorPosition (AbstractCursorPosition cursorPosition)
            {
            }

            void HandleModEnter (dynamic self, dynamic args)
                => EventsObserver.OnNext (
                    new InsertCellEvent<CodeCell> (Cell));

            dynamic HandleCursorUpDown (dynamic self, dynamic args)
            {
                var isUp = (bool)args [0];
                // var isMod = (bool)args [1];

                FocusSiblingEditorEvent evnt = null;

                if (isUp && proseMirror.shouldFocusPreviousEditor ())
                    evnt = new FocusSiblingEditorEvent (
                        this,
                        FocusSiblingEditorEvent.WhichEditor.Previous);
                else if (!isUp && proseMirror.shouldFocusNextEditor ())
                    evnt = new FocusSiblingEditorEvent (
                        this,
                        FocusSiblingEditorEvent.WhichEditor.Next);

                if (evnt == null)
                    return false;

                EventsObserver.OnNext (evnt);
                return true;
            }

            public override void Focus () => proseMirror.focus ();

            public string MarkdownContent {
                get { return proseMirror.content; }
                set { proseMirror.content = value; }
            }

            void HandleChange (dynamic self, dynamic args)
                => EventsObserver.OnNext (new ChangeEvent (this));

            void HandleFocus (dynamic self, dynamic args)
                => EventsObserver.OnNext (new FocusEvent (this));

            #region Commands

            EditorCommand ToEditorCommand (dynamic commandSpec, string id)
                => new EditorCommand (id, commandSpec.label, commandSpec.title);

            dynamic GetCommandSpec (string commandId)
                => proseMirror?.getMenuItems ()?.GetProperty (commandId)?.spec;

            public override bool TryGetCommand (string commandId, out EditorCommand command)
            {
                var commandSpec = GetCommandSpec (commandId);
                if (commandSpec == null) {
                    command = default (EditorCommand);
                    return false;
                }

                command = ToEditorCommand (commandSpec, commandId);
                return true;
            }

            public override IEnumerable<EditorCommand> GetCommands ()
            {
                var commands = proseMirror.getMenuItems ();
                foreach (var id in commands.GetPropertyNames ())
                    yield return ToEditorCommand (commands.GetProperty (id), id);
            }

            public override EditorCommandStatus GetCommandStatus (EditorCommand command)
                => GetCommandSpec (command.Id)?.select (proseMirror.pm) ?? false
                    ? EditorCommandStatus.Enabled
                    : EditorCommandStatus.Disabled;

            public override void ExecuteCommand (EditorCommand command)
                => GetCommandSpec (command.Id)?.run (proseMirror.pm);

            #endregion
        }
    }
}