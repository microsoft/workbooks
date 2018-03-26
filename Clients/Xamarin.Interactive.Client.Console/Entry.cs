//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mono.Options;
using Mono.Terminal;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Models;
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
        static LanguageDescription language;
        static CodeCellId lastCodeCellId;
        static CodeCellEvaluationStatus lastCellEvaluationStatus;
        static Spinner evalSpinner;

        static async Task<int> MainAsync (string [] args)
        {
            ClientSessionUri sessionUri = null;
            TextWriter logWriter = null;
            var showHelp = false;

            var options = new OptionSet {
                { "usage: xic [OPTIONS]+ [URI]"},
                { "" },
                { "Options:" },
                { "" },
                { "l|log=", "Write debugging log to file",
                    v => logWriter = new StreamWriter (v) },
                { "h|help", "Show this help",
                    v => showHelp = true }
            };

            try {
                args = options.Parse (args).ToArray ();
            } catch (Exception e) {
                Error.WriteLine ($"Invalid option: {e.Message}");
                showHelp = true;
            }

            if (showHelp) {
                options.WriteOptionDescriptions (Out);
                return 1;
            }

            if (args.Length > 0) {
                if (!ClientSessionUri.TryParse (args [0], out sessionUri)) {
                    Error.WriteLine ($"Invalid URI: {args [0]}");
                    return 1;
                }
            }

            // set up the very basic global services/environment
            var clientApp = new ConsoleClientApp ();
            clientApp.Initialize (
                logProvider: new LogProvider (LogLevel.Info, logWriter));

            // Now create and get ready to deal with the session; a more complete
            // client should handle more than just OnNext from the observer.
            var session = new InteractiveSession (agentUri: sessionUri);
            session.Events.Subscribe (new Observer<InteractiveSessionEvent> (OnSessionEvent));

            if (sessionUri?.WorkbookPath != null)
                await WorkbookPlayerMain (session, sessionUri);
            else
                await ReplPlayerMain (session);

            // Nevermind this... it'll get fixed!
            await Task.Delay (Timeout.Infinite);
            return 0;
        }

        /// <summary>
        /// Hosts an interactive REPL against a supported Workbooks target platform.
        /// This is analogous to 'csharp' or 'csi' or any other REPL on the planet.
        /// </summary>
        static async Task<int> ReplPlayerMain (InteractiveSession session)
        {
            // As an exercise to the reader, this puppy should take an optional
            // workbook flavor ID to know what platform you want to REPL and find
            // it in the list of installed and available ones...
            // For now we'll just pick the first available option 😺
            var workbookTarget = WorkbookAppInstallation.All.FirstOrDefault ();
            if (workbookTarget == null) {
                RenderError ("No workbook target platforms could be found.");
                return 1;
            }

            // We do not currently expose a list of available language descriptions
            // for the given build/installation, but if we did, this is when
            // you'd want to pick one. Just assume 'csharp' for now. Stay tuned.
            language = "csharp";

            // A session description combines just enough info for the entire
            // EvaluationService to get itself in order to do your bidding.
            var sessionDescription = new InteractiveSessionDescription (
                language,
                workbookTarget.Id,
                new EvaluationEnvironment (Environment.CurrentDirectory));

            // And initialize it with all of our prerequisites...
            // Status events raised within this method will be posted to the
            // observable above ("starting agent", "initializing workspace", etc).
            await session.InitializeAsync (sessionDescription);

            CodeCellId cellId = default;

            var editor = new LineEditor ("xic");
            editor.BeforeRenderPrompt = () => ForegroundColor = ConsoleColor.Yellow;
            editor.AfterRenderPrompt = () => ResetColor ();

            // At this point we have the following in order, ready to serve:
            //
            //   1. a connected agent ready to execute code
            //   2. a workspace that can perform compliation, intellisense, etc
            //   3. an evaluation service that is ready to deal with (1) and (2)
            //
            // It's at this point that a full UI would allow the user to actually
            // run code. This is the "User Experience main()"...
            //
            // This is the REPL you're looking for...
            while (true) {
                // append a new cell (no arguments here imply append)
                cellId = await session.EvaluationService.InsertCodeCellAsync ();

                for (int i = 0; true; i++) {
                    var deltaBuffer = editor.Edit (
                        GetPrompt (i > 0),
                        null);

                    var existingBuffer = session.WorkspaceService.GetCellBuffer (cellId);

                    await session.EvaluationService.UpdateCodeCellAsync (
                        cellId,
                        existingBuffer + deltaBuffer);

                    if (session.WorkspaceService.IsCellComplete (cellId))
                        break;
                }

                var finishedEvent = await session.EvaluationService.EvaluateAsync (cellId);

                // if the evaluation was not successful, remove the cell so it's not internally
                // re-evaluated (which would continue to yield the same failures)
                if (finishedEvent.Status != CodeCellEvaluationStatus.Success)
                    await session.EvaluationService.RemoveCodeCellAsync (finishedEvent.CodeCellId);
            }
        }

        /// <summary>
        /// Provides very basic workbook parsing and shunting of code cells
        /// into the evaluation service. Does not display non-code-cell contents
        /// but does evaluate a workbook from top-to-bottom. Restores nuget
        /// packages from the workbook's manifest.
        /// </summary>
        static async Task<int> WorkbookPlayerMain (InteractiveSession session, ClientSessionUri sessionUri)
        {
            var path = new FilePath (sessionUri.WorkbookPath);
            if (!path.FileExists) {
                Error.WriteLine ($"File does not exist: {path}");
                return 1;
            }

            // load the workbook file
            var workbook = new WorkbookPackage (path);
            await workbook.Open (
                quarantineInfo => Task.FromResult (true),
                path);

            #pragma warning disable 0618
            // TODO: WorkbookDocumentManifest needs to eliminate AgentType like we've done on web
            // to avoid having to use the the flavor mapping in AgentIdentity.
            var targetPlatformIdentifier = AgentIdentity.GetFlavorId (workbook.PlatformTargets [0]);
            #pragma warning restore 0618

            // initialize the session based on manifest metadata from the workbook file
            language = workbook.GetLanguageDescriptions ().First ();
            await session.InitializeAsync (new InteractiveSessionDescription (
                language,
                targetPlatformIdentifier,
                new EvaluationEnvironment (Environment.CurrentDirectory)));

            // restore NuGet packages
            await session.PackageManagerService.RestoreAsync (
                workbook.Pages.SelectMany (page => page.Packages));

            // insert and evaluate cells in the workbook
            foreach (var cell in workbook.IndexPage.Contents.OfType<CodeCell> ()) {
                var buffer = cell.Buffer.Value;

                lastCodeCellId = await session.EvaluationService.InsertCodeCellAsync (
                    buffer,
                    lastCodeCellId);

                ForegroundColor = ConsoleColor.DarkYellow;
                Write (GetPrompt ());
                ResetColor ();

                WriteLine (buffer);

                await session.EvaluationService.EvaluateAsync (lastCodeCellId);

                if (lastCellEvaluationStatus != CodeCellEvaluationStatus.Success)
                    break;
            }

            return 0;
        }

        #region Respond Nicely To Session Events

        static void OnSessionEvent (InteractiveSessionEvent evnt)
        {
            string status;

            switch (evnt.Kind) {
            case InteractiveSessionEventKind.Evaluation:
                OnCodeCellEvent ((ICodeCellEvent)evnt.Data);
                return;
            case InteractiveSessionEventKind.ConnectingToAgent:
                status = "Connecting to agent…";
                break;
            case InteractiveSessionEventKind.InitializingWorkspace:
                status = "Initializing workspace…";
                break;
            case InteractiveSessionEventKind.Ready:
                status = string.Empty.PadLeft (BufferWidth);
                break;
            default:
                return;
            }

            ForegroundColor = ConsoleColor.Cyan;
            Write ($"{status}\r");
        }

        static void OnCodeCellEvent (ICodeCellEvent codeCellEvent)
        {
            // NOTE: events may post to cells "out of order" from evaluation in
            // special cases such as reactive/IObservable integrations. This is
            // not handled at all in this simple console client since we just
            // append to stdout. Because of this, ignore "out of order" cell
            // events entirely. A real UI would render them in the correct place.
            if (lastCodeCellId != default && codeCellEvent.CodeCellId != lastCodeCellId)
                return;

            if (codeCellEvent is CodeCellEvaluationStartedEvent)
                StartSpinner ();
            else
                StopSpinner ();

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
                RenderResult (result);
                break;
            }
        }

        #endregion

        #region Amazing UI

        static void StartSpinner ()
        {
            StopSpinner ();
            evalSpinner = Spinner.Start (Spinner.Kind.Dots, ConsoleColor.Green);
        }

        static void StopSpinner ()
        {
            evalSpinner?.Dispose ();
            evalSpinner = null;
        }

        static string GetPrompt (bool secondaryPrompt = false)
        {
            var prompt = language.Name;
            if (secondaryPrompt)
                prompt = string.Empty.PadLeft (prompt.Length);
            return prompt + "> ";
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

        static void RenderResult (CodeCellResultEvent result)
        {
            ForegroundColor = ConsoleColor.Magenta;
            Write (result.Type.Name);
            Write (": ");
            ResetColor ();

            // A full client would implement real result rendering and interaction,
            // but console client only cares about showing the ToString representation
            // right now. We will always serialize a ReflectionInteractiveObject as
            // a representation of a result which always contains the result of calling
            // .ToString on that value. Find it and display that. If that's not available,
            // then the result was null or something unexpected happened in eval.
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

        static void RenderDiagnostic (Diagnostic diagnostic)
        {
            switch (diagnostic.Severity) {
            case DiagnosticSeverity.Warning:
                ForegroundColor = ConsoleColor.DarkYellow;
                Write ($"warning ({diagnostic.Id}): ");
                break;
            case DiagnosticSeverity.Error:
                ForegroundColor = ConsoleColor.Red;
                Write ($"error ({diagnostic.Id}): ");
                break;
            default:
                return;
            }

            ResetColor ();

            var (line, column) = diagnostic.Range;
            WriteLine ($"({line},{column}): {diagnostic.Message}");
        }

        #endregion

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
    }
}