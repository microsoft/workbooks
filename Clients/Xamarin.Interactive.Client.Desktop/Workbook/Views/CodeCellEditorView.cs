//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.CodeAnalysis.Monaco;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Workbook.Events;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Views
{
    sealed class CodeCellEditorView : CellEditorView
    {
        readonly CodeCellState state;
        readonly ScriptContext jsContext;
        readonly IDisposable preferenceSubscription;

        #pragma warning disable 0414
        readonly dynamic codeEditor;
        #pragma warning restore 0414

        public bool IsReadOnly {
            get { return codeEditor.isReadOnly (); }
            set {
                codeEditor.setReadOnly (value);
            }
        }

        public override Cell Cell { get; }

        public CodeCellEditorView (
            CodeCellState codeCellState,
            CodeCell codeCell,
            HtmlElement element)
        {
            if (codeCellState == null)
                throw new ArgumentNullException (nameof (codeCellState));

            if (codeCell == null)
                throw new ArgumentNullException (nameof (codeCell));

            if (element == null)
                throw new ArgumentNullException (nameof (element));

            this.state = codeCellState;
            this.jsContext = element.Context;

            Cell = codeCell;

            codeEditor = jsContext.GlobalObject.xiexports.monaco.WorkbookCodeEditor (jsContext.CreateObject (o => {
                o.placeElem = element;
                o.readOnly = false;
                o.fontSize = Prefs.UI.Font.GetSize ();
                o.showLineNumbers = Prefs.Editor.ShowLineNumbers.GetValue ();
                o.onFocus = (ScriptAction)HandleFocus;
                o.onChange = (ScriptAction)HandleChange;
                o.onCursorUpDown = (ScriptFunc)HandleCursorUpDown;
                o.onEnter = (ScriptFunc)HandleEnter;
                o.theme = GetThemeName ();
                o.wrapLongLines = Prefs.Submissions.WrapLongLinesInEditor.GetValue ();
            }));

            codeEditor.setText (codeCell.Buffer.Value);

            preferenceSubscription = PreferenceStore.Default.Subscribe (change => {
                if (change.Key == Prefs.UI.Font.Key)
                    // TODO: Figure out why this doesn't appear to update font size in completion window
                    codeEditor.setFontSize (Prefs.UI.Font.GetSize ());
                else if (change.Key == Prefs.Editor.ShowLineNumbers.Key)
                    codeEditor.setShowLineNumbers (Prefs.Editor.ShowLineNumbers.GetValue ());
                else if (change.Key == Prefs.UI.Theme.UseHighContrast.Key || change.Key == Prefs.UI.Theme.ThemeName.Key)
                    codeEditor.setTheme (GetThemeName ());
                else if (change.Key == Prefs.Submissions.WrapLongLinesInEditor.Key)
                    codeEditor.setWordWrap (Prefs.Submissions.WrapLongLinesInEditor.GetValue ());
            });
        }

        string GetThemeName ()
        {
            var themeName = Prefs.UI.Theme.ThemeName.GetValue ();
            var useHighContrast = Prefs.UI.Theme.UseHighContrast.GetValue ();

            // On Windows, let Monaco do its own thing, don't force it to a theme.
            if (useHighContrast && ClientApp.SharedInstance.Host.IsMac)
                return "hc-black";

            switch (themeName) {
            case "Dark":
                return "vs-dark";
            default:
                return "vs";
            }
        }

        public string GetMonacoModelId () => codeEditor.getModelId ().ToString ();

        public override void Focus () => codeEditor.focus ();

        public override void SetCursorPosition (AbstractCursorPosition cursorPosition)
            => codeEditor.setCursorPosition ((int)cursorPosition);

        void HandleFocus (dynamic self, dynamic args)
        {
            EventsObserver.OnNext (new FocusEvent (this));
        }

        public Position CursorPosition {
            get {
                return MonacoExtensions.FromMonacoPosition (codeEditor.mEditor.getPosition ());
            }

            set {
                codeEditor.mEditor.setPosition (jsContext.ToMonacoPosition (value));
            }
        }

        public override void OnBlur ()
            => codeEditor.dismissParameterHintsWindow ();

        public void ClearMarkedText () => codeEditor.clearMarkedText ();

        void MarkText (PositionSpan span,
            string className = null,
            string title = null)
            => codeEditor.markText (jsContext.ToMonacoRange (span), className, title);

        dynamic HandleEnter (dynamic self, dynamic args)
        {
            var isShift = (bool)args [0];
            var isMeta = (bool)args [1];
            var isCtrl = (bool)args [2];

            var isMod = HostEnvironment.OS == HostOS.macOS ? isMeta : isCtrl;

            // Shift+Mod+Enter: new markdown cell
            if (isShift && isMod) {
                EventsObserver.OnNext (
                    new InsertCellEvent<MarkdownCell> (Cell));
                return true;
            }

            // Mod+Enter: evaluate
            if (isMod) {
                EvaluateViaKeyPress ();
                return true;
            }

            // Shift+Enter: regular newline+indent
            if (isShift)
                return false;

            // Regular enter: evaluate if cell submission complete
            var shouldSubmit =
                Prefs.Submissions.ReturnAtEndOfBufferExecutes.GetValue () &&
                !codeEditor.isSomethingSelected () &&
                codeEditor.isCursorAtEnd ();

            var workspace = state.CompilationWorkspace;

            if (shouldSubmit &&
                !String.IsNullOrWhiteSpace (Cell.Buffer.Value) &&
                  workspace != null &&
                workspace.IsCellComplete (state.CodeCellId)) {
                EventsObserver.OnNext (new EvaluateCodeCellEvent (this));
                return true;
            }

            return false;
        }

        public void EvaluateViaKeyPress ()
        {
            if (!String.IsNullOrWhiteSpace (Cell.Buffer.Value))
                EventsObserver.OnNext (new EvaluateCodeCellEvent (this));
        }

        dynamic HandleCursorUpDown (dynamic self, dynamic args)
        {
            var isUp = (bool)args [0];

            var pos = CursorPosition;

            var historyEvent = new NavigateReplHistoryEvent (this, pos, isUp);
            EventsObserver.OnNext (historyEvent);
            if (historyEvent.Handled)
                return true;

            if (isUp && pos.LineNumber == 0) {
                EventsObserver.OnNext (new FocusSiblingEditorEvent (
                    this,
                    FocusSiblingEditorEvent.WhichEditor.Previous));
                return true;
            }

            if (!isUp && pos.LineNumber == codeEditor.getLastLineIndex ()) {
                EventsObserver.OnNext (new FocusSiblingEditorEvent (
                    this,
                    FocusSiblingEditorEvent.WhichEditor.Next));
                return true;
            }

            return false;
        }

        void HandleChange (dynamic self, dynamic args)
        {
            Cell.Buffer.Value = codeEditor.getText ();

            // WorkbookCodeEditorChangeEvent
            var changeEvent = args [0];
            var text = changeEvent.text?.ToString () ?? "";

            EventsObserver.OnNext (new ChangeEvent (this, text));

            UpdateDiagnosticsAsync ().Forget ();
        }

        protected override void Dispose (bool disposing)
        {
            EventsObserver.OnNext (new DeleteCellEvent<CodeCell> ((CodeCell) Cell));
            preferenceSubscription.Dispose ();
            codeEditor.dispose ();
        }

        #region Inline Diagnostics

        async Task UpdateDiagnosticsAsync (CancellationToken cancellationToken = default (CancellationToken))
        {
            ClearMarkedText ();

            var workspace = state.CompilationWorkspace;
            if (workspace == null)
                return;

            var diagnostics = await workspace.GetCellDiagnosticsAsync (
                state.CodeCellId,
                cancellationToken);

            foreach (var diag in diagnostics) {
                if (diag.Severity == InteractiveDiagnosticSeverity.Error)
                    MarkText (
                        diag.Span,
                        "CodeMirror-diagnostic",
                        diag.Message);
            }
        }

        #endregion
    }
}