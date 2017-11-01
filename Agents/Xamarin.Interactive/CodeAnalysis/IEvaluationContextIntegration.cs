//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis
{
    public interface IEvaluationContextIntegration
    {
        void IntegrateWith (IEvaluationContext evaluationContext);
    }
}