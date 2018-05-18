//
// Authors:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Json;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Workbooks.WebAssembly
{
    sealed class WebAssemblyAgent : Agent
    {
        const string TAG = nameof (WebAssemblyAgent);
        readonly WasmLogProvider logProvider = new WasmLogProvider (Log.GetLogLevel ());
        readonly WebAssemblyEvaluationObserver evalObserver = new WebAssemblyEvaluationObserver ();
        readonly JsonSerializerSettings serializerSettings = InteractiveJsonSerializerSettings.SharedInstance;

        EvaluationContextId evalContextId;

        public WebAssemblyAgent () : this (false)
        {
        }

        public WebAssemblyAgent (bool unitTestContext = false) : base (unitTestContext)
        {
            Log.EntryAdded += (sender, entry) => logProvider.Commit (entry);
            base.EvaluationContextManager.Events.Subscribe (evalObserver);
        }

        protected override EvaluationContextManager CreateEvaluationContextManager ()
            => new WebAssemblyEvaluationContextManager (this);

        public void Initialize (string targetCompilationConfigurationJson)
        {
            var targetCompilationConfiguration = JsonConvert.DeserializeObject<TargetCompilationConfiguration> (
                targetCompilationConfigurationJson,
                serializerSettings);
            evalContextId = targetCompilationConfiguration.EvaluationContextId;
            base.EvaluationContextManager.CreateEvaluationContextAsync (targetCompilationConfiguration);
        }

        public void Evaluate (string compilationJson)
        {
            var compilation = JsonConvert.DeserializeObject<Compilation> (compilationJson, serializerSettings);
            base.EvaluationContextManager.EvaluateAsync (evalContextId, compilation);
        }

        public override InspectView GetVisualTree (string hierarchyKind)
            => throw new NotSupportedException ();
    }
}