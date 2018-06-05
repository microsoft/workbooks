//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    public sealed class EvaluationContext
    {
        readonly List<AssemblyDefinition> loadedAssemblies = new List<AssemblyDefinition> ();
        object [] submissionStates;
        volatile Thread currentRunThread;
        bool initializedIntegration;

        readonly Observable<ICodeCellEvent> events = new Observable<ICodeCellEvent> ();
        public IObservable<ICodeCellEvent> Events => events;

        public EvaluationContextManager Host { get; }
        internal EvaluationAssemblyContextBase AssemblyContext { get; }

        internal EvaluationContext (
            EvaluationContextManager host,
            EvaluationAssemblyContextBase assemblyContext,
            object globalState)
        {
            Host = host
                ?? throw new ArgumentNullException (nameof (host));

            AssemblyContext = assemblyContext
                ?? throw new ArgumentNullException (nameof (assemblyContext));

            AssemblyContext.AssemblyResolvedHandler = HandleAssemblyResolved;

            submissionStates = new [] { globalState, null };
        }

        internal void Dispose ()
        {
            events.Observers.OnCompleted ();

            AssemblyContext.Dispose ();
            GC.SuppressFinalize (this);
        }

        void HandleAssemblyResolved (Assembly assembly, AssemblyDefinition remoteAssembly)
        {
            initializedIntegration |= Host.TryLoadIntegration (assembly);
            loadedAssemblies.Add (remoteAssembly);
            Host.LoadExternalDependencies (assembly, remoteAssembly.ExternalDependencies);
        }

        internal async Task EvaluateAsync (
            Compilation compilation,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            foreach (var assembly in new AssemblyDefinition [] { compilation.ExecutableAssembly }
                .Concat (compilation.References ?? new AssemblyDefinition [] { }))
                Host.LoadExternalDependencies (null, assembly?.ExternalDependencies);

            var status = EvaluationStatus.Success;
            Exception evaluationException = null;

            var inFlight = EvaluationInFlight.Create (compilation);

            loadedAssemblies.Clear ();

            var savedStdout = Console.Out;
            var savedStderr = Console.Error;

            var capturedOutputWriter = new CapturedOutputWriter (compilation.CodeCellId);
            Console.SetOut (capturedOutputWriter.StandardOutput);
            Console.SetError (capturedOutputWriter.StandardError);
            capturedOutputWriter.SegmentCaptured += CapturedOutputWriter_SegmentCaptured;

            var stopwatch = new Stopwatch ();

            currentRunThread = Thread.CurrentThread;
            currentRunThread.CurrentCulture = InteractiveCulture.CurrentCulture;
            currentRunThread.CurrentUICulture = InteractiveCulture.CurrentUICulture;

            initializedIntegration = false;

            try {
                // We _only_ want to capture exceptions from user-code here but
                // allow our own bugs to propagate out to the normal exception
                // handling. The only reason we capture these exceptions is in
                // case anything has been written to stdout/stderr by user-code
                // before the exception was raised... we still want to send that
                // captured output back to the client for rendering.
                stopwatch.Start ();
                inFlight = inFlight.With (originalValue: await CoreEvaluateAsync (compilation, cancellationToken));
            } catch (AggregateException e) when (e.InnerExceptions?.Count == 1) {
                // the Roslyn-emitted script delegates are async, so all exceptions
                // raised within a delegate should be AggregateException; if there's
                // just one inner exception, unwind it since the async nature of the
                // script delegates is a REPL implementation detail.
                evaluationException = e.InnerExceptions [0];
            } catch (ThreadAbortException e) {
                evaluationException = e;
                status = EvaluationStatus.Interrupted;
                Thread.ResetAbort ();
            } catch (ThreadInterruptedException e) {
                evaluationException = e;
                status = EvaluationStatus.Interrupted;
            } catch (Exception e) {
                evaluationException = e;
            } finally {
                stopwatch.Stop ();
                currentRunThread = null;

                inFlight = inFlight.With (phase: EvaluationPhase.Evaluated);
                events.Observers.OnNext (inFlight);

                capturedOutputWriter.SegmentCaptured -= CapturedOutputWriter_SegmentCaptured;
                Console.SetOut (savedStdout);
                Console.SetError (savedStderr);
            }

            var resultHandling = EvaluationResultHandling.Replace;
            object result;

            if (evaluationException == null) {
                result = inFlight.OriginalValue;

                if (status == EvaluationStatus.Interrupted || (result == null && !compilation.IsResultAnExpression))
                    resultHandling = EvaluationResultHandling.Ignore;
            } else if (status == EvaluationStatus.Interrupted) {
                result = null;
            } else {
                result = evaluationException;
                status = EvaluationStatus.EvaluationException;
            }

            var evaluation = new Evaluation (
                compilation.CodeCellId,
                status,
                resultHandling,
                // an exception in the call to Prepare should not be explicitly caught
                // here (see above) since it'll be handled at a higher level and can be
                // flagged as being a bug in our code since this method should never throw.
                Host.RepresentationManager.Prepare (result),
                stopwatch.Elapsed,
                InteractiveCulture.CurrentCulture.LCID,
                InteractiveCulture.CurrentUICulture.LCID,
                initializedIntegration,
                loadedAssemblies.ToArray ());

            events.Observers.OnNext (evaluation);

            inFlight = inFlight.With (EvaluationPhase.Completed, evaluation: evaluation);
            events.Observers.OnNext (inFlight);
        }

        async Task<object> CoreEvaluateAsync (
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            if (compilation == null)
                throw new ArgumentNullException (nameof(compilation));

            if (compilation.References != null)
                AssemblyContext.AddRange (compilation.References);

            if (compilation?.ExecutableAssembly?.Content.PEImage == null)
                return null;

            var assembly = Assembly.Load (compilation.ExecutableAssembly.Content.PEImage);
            AssemblyContext.Add (assembly);

            var entryPointType = assembly.GetType (
                compilation.ExecutableAssembly.EntryPoint.TypeName, true);

            var entryPointMethod = entryPointType.GetMethod (
                compilation.ExecutableAssembly.EntryPoint.MethodName);

            var submission = (Func<object[], Task>)entryPointMethod.CreateDelegate (
                typeof (Func<object[], Task>));

            if (compilation.SubmissionNumber >= submissionStates.Length)
                Array.Resize (
                    ref submissionStates,
                    Math.Max (compilation.SubmissionNumber, submissionStates.Length * 2));

            cancellationToken.ThrowIfCancellationRequested ();

            return await ((Task<object>)submission (submissionStates)).ConfigureAwait (false);
        }

        internal void AbortEvaluation ()
            => currentRunThread?.Abort ();

        void CapturedOutputWriter_SegmentCaptured (CapturedOutputSegment segment)
            => events.Observers.OnNext (segment);

        internal struct GlobalVariable
        {
            public FieldInfo Field { get;}
            public object Value { get; }
            public Exception ValueReadException { get; }

            public GlobalVariable (
                FieldInfo field,
                object value,
                Exception valueReadException)
            {
                Field = field;
                Value = value;
                ValueReadException = valueReadException;
            }
        }

        internal IReadOnlyCollection<GlobalVariable> GetGlobalVariables ()
        {
            var globalVars = new Dictionary<string, GlobalVariable> ();

            for (int i = 1; i < submissionStates.Length; i++) {
                var state = submissionStates [i];
                if (state == null)
                    continue;

                foreach (var field in state.GetType ().GetFields (
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
                    // <host-object> and <Script> fields are readonly
                    if (field.IsInitOnly)
                        continue;

                    object value = null;
                    Exception exception = null;

                    try {
                        value = field.GetValue (state);
                    } catch (Exception e) {
                        exception = e;
                    }

                    globalVars [field.Name] = new GlobalVariable (
                        field,
                        value,
                        exception);
                }
            }

            return globalVars.Values as IReadOnlyCollection<GlobalVariable>;
        }
    }
}