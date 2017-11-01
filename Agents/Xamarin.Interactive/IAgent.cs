//
// IAgent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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