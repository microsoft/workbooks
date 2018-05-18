//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Client.Web.WebAssembly
{
    // TODO: Implement parts of this that make sense for WASM. Interact/GetObjectMembers/SetObjectMembers
    // and SetLogLevel should be doable, the rest are, like the Console client, not going to work. - brajkovic
    sealed class WebAssemblyAgentClient : IAgentClient
    {
        CancellationToken sessionCancellationToken;

        public IEvaluationContextManager EvaluationContextManager { get; }

        public WebAssemblyAgentClient ()
            => EvaluationContextManager = new WebAssemblyEvaluationContextManager ();

        public Task<AgentFeatures> GetAgentFeaturesAsync (CancellationToken cancellationToken = default)
            => Task.FromResult (new AgentFeatures (Array.Empty<string> ()));

        public Task<InteractiveObject> GetObjectMembersAsync (
            long viewHandle,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task<InspectView> GetVisualTreeAsync (
            string hierarchyKind,
            bool captureViews = true,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task<T> HighlightView<T> (
            double x,
            double y,
            bool clear,
            string hierarchyKind,
            CancellationToken cancellationToken = default)
            where T : InspectView
            => throw new NotImplementedException ();

        public Task<IInteractiveObject> InteractAsync (
            IInteractiveObject obj,
            object message,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task SetLogLevelAsync (
            LogLevel newLogLevel,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<SetObjectMemberResponse> SetObjectMemberAsync (
            long objHandle,
            RepresentedMemberInfo memberInfo,
            object value,
            bool returnUpdatedValue,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public void UpdateSessionCancellationToken (CancellationToken cancellationToken)
            => sessionCancellationToken = cancellationToken;
    }
}