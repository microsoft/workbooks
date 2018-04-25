// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Client
{
    interface IAgentClient
    {
        void UpdateSessionCancellationToken (CancellationToken cancellationToken);

        IEvaluationContextManager EvaluationContextManager { get; }

        Task<AgentFeatures> GetAgentFeaturesAsync (
            CancellationToken cancellationToken = default);

        Task SetLogLevelAsync (
            LogLevel newLogLevel,
            CancellationToken cancellationToken = default);


        Task<InspectView> GetVisualTreeAsync (
            string hierarchyKind,
            bool captureViews = true,
            CancellationToken cancellationToken = default);

        Task<InteractiveObject> GetObjectMembersAsync (
            long viewHandle,
            CancellationToken cancellationToken = default);

        Task<SetObjectMemberResponse> SetObjectMemberAsync (
            long objHandle,
            RepresentedMemberInfo memberInfo,
            object value,
            bool returnUpdatedValue,
            CancellationToken cancellationToken = default);

        Task<T> HighlightView<T> (
            double x,
            double y,
            bool clear,
            string hierarchyKind,
            CancellationToken cancellationToken = default)
            where T : InspectView;

        Task<IInteractiveObject> InteractAsync (
            IInteractiveObject obj,
            object message,
            CancellationToken cancellationToken = default);
    }
}