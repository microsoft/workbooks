//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
    public interface IEvaluationContext
    {
        EvaluationContextId Id { get; }
        IObservable<IEvaluation> Evaluations { get; }
    }
}