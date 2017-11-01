//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive
{
    public interface IAgent
    {
        IRepresentationManager RepresentationManager { get; }

        IAgentSynchronizationContext SynchronizationContexts { get; }

        Func<object> CreateDefaultHttpMessageHandler { get; set; }

        void RegisterResetStateHandler (Action handler);

        void PublishEvaluation (
            CodeCellId codeCellId,
            object result,
            EvaluationResultHandling resultHandling = EvaluationResultHandling.Replace);
    }
}