//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Session;
using Xamarin.Interactive.Workbook.Models;
using Xamarin.Interactive.Representations;

using static System.Console;

namespace Xamarin.Interactive.Client.Console
{
    static class Entry
    {
        static int Main (string [] args)
        {
            var runContext = new SingleThreadSynchronizationContext ();
            SynchronizationContext.SetSynchronizationContext (runContext);

            var mainTask = MainAsync (args);

            mainTask.ContinueWith (
                task => runContext.Complete (),
                TaskScheduler.Default);

            runContext.RunOnCurrentThread ();

            return mainTask.GetAwaiter ().GetResult ();
        }

        static async Task<int> MainAsync (string [] args)
        {
            // handle command line arguments
            if (args.Length == 0 || args [0] == null) {
                Error.WriteLine ("usage: WORKBOOK_PATH");
                return 1;
            }

            var path = new FilePath (args [0]);
            if (!path.FileExists) {
                Error.WriteLine ($"File does not exist: {path}");
                return 1;
            }

            // set up the very basic global services/environment
            var clientApp = new ConsoleClientApp ();
            clientApp.Initialize (
                logProvider: new LogProvider (LogLevel.Info, null));

            // load the workbook file
            var workbook = new WorkbookPackage (path);
            await workbook.Open (
                quarantineInfo => Task.FromResult (true),
                path);

            // create and get ready to deal with the session; a more complete
            // client should handle more than just OnNext from the observer.
            var session = InteractiveSession.CreateWorkbookSession ();
            session.Events.Subscribe (new Observer<InteractiveSessionEvent> (OnSessionEvent));
            //CodeCellId lastEvaluatingCellId = default; // used in OnSessionEvent below

            #pragma warning disable 0618
            // TODO: WorkbookDocumentManifest needs to eliminate AgentType like we've done on web
            // to avoid having to use the the flavor mapping in AgentIdentity.
            var targetPlatformIdentifier = AgentIdentity.GetFlavorId (workbook.PlatformTargets [0]);
            #pragma warning restore 0618

            // initialize the session based on manifest metadata from the workbook file
            var language = workbook.GetLanguageDescriptions ().First ();
            await session.InitializeAsync (new InteractiveSessionDescription (
                language,
                targetPlatformIdentifier,
                new EvaluationEnvironment (Environment.CurrentDirectory)));

            CodeCellId lastCodeCellId = default;
            CodeCellEvaluationStatus lastCellEvaluationStatus = default;

            // restore NuGet packages
            await session.PackageManagerService.RestoreAsync (
                workbook.Pages.SelectMany (page => page.Packages));

            // insert and evaluate cells in the workbook
            foreach (var cell in workbook.IndexPage.Contents.OfType<CodeCell> ()) {
                var buffer = cell.CodeAnalysisBuffer.Value;

                lastCodeCellId = await session.EvaluationService.InsertCodeCellAsync (
                    buffer,
                    lastCodeCellId);

                RenderBuffer (language, buffer);

                await session.EvaluationService.EvaluateAsync (lastCodeCellId);

                if (lastCellEvaluationStatus != CodeCellEvaluationStatus.Success)
                    break;
            }

            await Task.Delay (2000);

            return 0;

            void OnSessionEvent (InteractiveSessionEvent evnt)
            {
                switch (evnt.Kind) {
                case InteractiveSessionEventKind.Evaluation:
                    var codeCellEvent = (ICodeCellEvent)evnt.Data;

                    // NOTE: events may post to cells "out of order" from evaluation in
                    // special cases such as reactive/IObservable integrations. This is
                    // not handled at all in this simple console client since we just
                    // append to stdout. Because of this, ignore "out of order" cell
                    // events entirely. A real UI would render them in the correct place.
                    if (lastCodeCellId != default &&
                        codeCellEvent.CodeCellId != lastCodeCellId)
                        break;

                    switch (codeCellEvent) {
                    // a full UI would set cell state to show a spinner and
                    // enable a button to abort the running evaluation here
                    case CodeCellEvaluationStartedEvent _:
                        break;
                    // and would then hide the spinner and button here
                    case CodeCellEvaluationFinishedEvent finishedEvent:
                        lastCellEvaluationStatus = finishedEvent.Status;

                        switch (finishedEvent.Status) {
                        case CodeCellEvaluationStatus.Disconnected:
                            RenderError ("Agent was disconnected while evaluating cell");
                            break;
                        case CodeCellEvaluationStatus.Interrupted:
                            RenderError ("Evaluation was aborted");
                            break;
                        case CodeCellEvaluationStatus.EvaluationException:
                            RenderError ("An exception was thrown while evaluating cell");
                            break;
                        }

                        foreach (var diagnostic in finishedEvent.Diagnostics)
                            RenderDiagnostic (diagnostic);

                        break;
                    // and would render console output and results on the cell itself
                    // instead of just appending to the screen (see "out of order" note)
                    case CapturedOutputSegment output:
                        RenderOutput (output);
                        break;
                    case CodeCellResultEvent result:
                        RenderResult (language, result);
                        break;
                    }

                    break;
                default:
                    ForegroundColor = ConsoleColor.Cyan;
                    WriteLine (evnt.Kind);
                    ResetColor ();
                    break;
                }
            }
        }

        static void RenderBuffer (LanguageDescription language, string buffer)
        {
            ForegroundColor = ConsoleColor.DarkYellow;
            Write ($"{language.Name}> ");
            ResetColor ();
            WriteLine (buffer);
        }

        static void RenderOutput (CapturedOutputSegment output)
        {
            switch (output.FileDescriptor) {
            case 1:
                ForegroundColor = ConsoleColor.Gray;
                break;
            case 2:
                ForegroundColor = ConsoleColor.Red;
                break;
            }

            Write (output.Value);

            ResetColor ();
        }

        static void RenderResult (LanguageDescription language, CodeCellResultEvent result)
        {
            ForegroundColor = ConsoleColor.Green;
            Write ($"{string.Empty.PadLeft (language.Name.Length)}> ");
            ResetColor ();

            ForegroundColor = ConsoleColor.Magenta;
            Write (result.Type.Name);
            Write (": ");
            ResetColor ();

            WriteLine (
                result
                    .ValueRepresentations
                    .OfType<ReflectionInteractiveObject> ()
                    .FirstOrDefault ()
                    ?.ToStringRepresentation ??
                        result.ValueRepresentations.FirstOrDefault () ??
                            "null");
        }

        static void RenderError (string message)
        {
            ForegroundColor = ConsoleColor.Red;
            Write ("Error: ");
            ResetColor ();
            WriteLine (message);
        }

        static void RenderDiagnostic (InteractiveDiagnostic diagnostic)
        {
            switch (diagnostic.Severity) {
            case Microsoft.CodeAnalysis.DiagnosticSeverity.Warning:
                ForegroundColor = ConsoleColor.DarkYellow;
                Write ($"warning ({diagnostic.Id}): ");
                break;
            case Microsoft.CodeAnalysis.DiagnosticSeverity.Error:
                ForegroundColor = ConsoleColor.Red;
                Write ($"error ({diagnostic.Id}): ");
                break;
            default:
                return;
            }

            ResetColor ();

            var (line, column) = diagnostic.Span;
            WriteLine ($"({line},{column}): {diagnostic.Message}");
        }
    }
}