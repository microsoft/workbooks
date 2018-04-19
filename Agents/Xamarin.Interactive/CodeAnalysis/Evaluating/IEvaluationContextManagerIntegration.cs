// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    public interface IEvaluationContextManagerIntegration
    {
        void IntegrateWith (EvaluationContextManager evaluationContextManager);
    }
}