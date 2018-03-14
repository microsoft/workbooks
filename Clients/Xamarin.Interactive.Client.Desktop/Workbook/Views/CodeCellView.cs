//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Events;
using Xamarin.Interactive.Rendering;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Workbook.Events;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Views
{
    sealed class CodeCellView : CellView, ICodeCellView
    {
        readonly Observable<IEvent> events = new Observable<IEvent> ();
        public IObservable<IEvent> Events => events;

        readonly CodeCell codeCell;
        readonly RendererContext rendererContext;
        readonly CodeCellEditorView editor;

        readonly HtmlElement editorElem;
        HtmlElement execControlElem;
        HtmlElement diagnosticsElem;
        HtmlElement capturedOutputElem;
        HtmlElement resultElem;
        HtmlElement evaluationDurationElem;

        public override IEditor Editor => editor;

        public CodeCell Cell => codeCell;

        public CodeCellView (
            Models.CodeCellState codeCellState,
            CodeCell codeCell,
            HtmlDocument document,
            RendererContext rendererContext) : base (
                document,
                "submission " + codeCell.LanguageName)
        {
            this.codeCell = codeCell
                ?? throw new ArgumentNullException (nameof (codeCell));

            this.rendererContext = rendererContext
                ?? throw new ArgumentNullException (nameof (rendererContext));

            ContentElement.AppendChild (editorElem = CreateContentContainer ("editor"));

            editor = new CodeCellEditorView (codeCellState, codeCell, editorElem);

            PreferenceStore.Default.Subscribe (change => {
                if (change.Key == Prefs.Submissions.ShowExecutionTimings.Key)
                    UpdateEvaluationDurationHidden ();
            });
        }

        bool WindowHasSelection {
            get { return !String.IsNullOrEmpty (Document.Context.GlobalObject.window.getSelection ().toString ()); }
        }

        public void Freeze ()
        {
            editor.IsReadOnly = true;
        }

        public bool IsFrozen => editor.IsReadOnly;

        public override void Focus (bool scrollIntoView = true)
        {
            editor.Focus ();

            if (!scrollIntoView)
                return;

            if (ClientApp.SharedInstance.Host.IsMac) {
                editorElem.ScrollIntoView ();
            } else {
                // scrollIntoView in IE will scroll the window horizontally if a previous REPL result is wider
                // than the window, so we implement it slightly differently.
                var boundingRect = editorElem.GetBoundingClientRect ();
                Document.Context.GlobalObject.window.scrollTo (0, editorElem.OffsetTop + boundingRect.Bottom);
            }
        }

        static void RemoveElement (ref HtmlElement element)
        {
            element?.ParentElement?.RemoveChild (element);
            element = null;
        }

        public void Reset ()
        {
            RemoveElement (ref execControlElem);
            RemoveElement (ref diagnosticsElem);
            RemoveElement (ref resultElem);
            RemoveElement (ref capturedOutputElem);
            RemoveElement (ref evaluationDurationElem);

            editor.ClearMarkedText ();

            HasErrorDiagnostics = false;
        }

        public bool IsEvaluating {
            get { return execControlElem != null; }
            set {
                RemoveElement (ref execControlElem);

                if (!value)
                    return;

                execControlElem = CreateContentContainer ("exec-control");
                execControlElem.AppendChild (Document.CreateElement ("div", "loader"));
                execControlElem.AddEventListener (
                    "click",
                    evnt => events.Observers.OnNext (new AbortEvaluationEvent (this)));

                var next = editorElem.NextElementSibling;
                if (next == null)
                    ContentElement.AppendChild (execControlElem);
                else
                    ContentElement.InsertBefore (execControlElem, next);
            }
        }

        void UpdateEvaluationDurationHidden ()
        {
            if (evaluationDurationElem != null) {
                if (Prefs.Submissions.ShowExecutionTimings.GetValue () &&
                    evaluationDuration != TimeSpan.Zero)
                    evaluationDurationElem.RemoveCssClass ("execution-duration-hidden");
                else
                    evaluationDurationElem.AddCssClass ("execution-duration-hidden");
            }
        }

        TimeSpan evaluationDuration;
        public TimeSpan EvaluationDuration {
            get { return evaluationDuration; }
            set {
                evaluationDuration = value;

                if (evaluationDurationElem == null)
                    ContentElement.AppendChild (evaluationDurationElem = Document.CreateElement (
                        "div",
                        @class: "execution-duration"));

                evaluationDurationElem.InnerHTML =
                    $"<span title=\"Cell evaluated in {evaluationDuration}\">" +
                    $"{evaluationDuration.ToPerformanceTimeString ()}</span>";

                UpdateEvaluationDurationHidden ();
            }
        }

        public bool HasErrorDiagnostics { get; set; }

        public bool IsDirty {
            get { return RootElement.HasCssClass ("dirty"); }
            set {
                if (value)
                    RootElement.AddCssClass ("dirty");
                else
                    RootElement.RemoveCssClass ("dirty");
            }
        }

        public bool IsOutdated {
            get { return ContentElement.HasCssClass ("outdated"); }
            set {
                if (value)
                    ContentElement.AddCssClass ("outdated");
                else
                    ContentElement.RemoveCssClass ("outdated");
            }
        }

        public void RenderDiagnostic (InteractiveDiagnostic diagnostic)
        {
            if (diagnosticsElem == null) {
                diagnosticsElem = CreateContentContainer ("diagnostics");
                if (HasErrorDiagnostics)
                    diagnosticsElem.AddCssClass ("error");
                ContentElement.AppendChild (diagnosticsElem);
            }

            var displayMessage = new StringBuilder ();
            var position = diagnostic.Span.ToRoslyn ().Start;
            var severity = diagnostic.Severity.ToString ().ToLowerInvariant ();

            var listElem = diagnosticsElem.FirstElementChild;
            if (listElem == null)
                diagnosticsElem.AppendChild (listElem = Document.CreateElement ("ul"));

            var itemElement = Document.CreateElement ("li", @class: severity);

            displayMessage.Append ($"({position.Line + 1},{position.Character + 1}): ");
            itemElement.AddEventListener ("click", evnt => {
                if (!WindowHasSelection) {
                    editor.Focus ();
                    editor.CursorPosition = position;
                }
            });

            displayMessage.Append (severity);

            if (!String.IsNullOrEmpty (diagnostic.Id))
                displayMessage.Append (' ').Append (diagnostic.Id);

            displayMessage.Append (": ").Append (diagnostic.Message);

            itemElement.AppendChild (Document.CreateTextNode (displayMessage.ToString ()));
            listElem.AppendChild (itemElement);
        }

        public void RenderResult (
            CultureInfo cultureInfo,
            object result,
            EvaluationResultHandling resultHandling)
        {
            if (resultElem == null)
                ContentElement.AppendChild (resultElem = CreateContentContainer ("result"));
            else if (resultHandling == EvaluationResultHandling.Replace)
                resultElem.RemoveChildren ();

            rendererContext.Render (
                RenderState.Create (result, cultureInfo),
                resultElem);
        }

        public void RenderCapturedOutputSegment (CapturedOutputSegment segment)
        {
            if (capturedOutputElem == null)
                ContentElement.AppendChild (
                    capturedOutputElem = CreateContentContainer ("captured-output"));

            var span = Document.CreateElement ("span",
                @class: segment.FileDescriptor == CapturedOutputWriter.StandardErrorFd
                ? "stderr"
                : "stdout");

            var builder = new StringBuilder ();

            for (var i = 0; i < segment.Value.Length; i++) {
                string escaped;
                var c = segment.Value [i];
                if (c.TryHtmlEscape (out escaped, true))
                    builder.Append (escaped);
                else
                    builder.Append (c);
            }

            span.InnerHTML = builder.ToString ();

            capturedOutputElem.AppendChild (span);
        }
    }
}