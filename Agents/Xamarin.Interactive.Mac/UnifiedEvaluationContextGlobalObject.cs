//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Unified
{
    public abstract class UnifiedEvaluationContextGlobalObject : EvaluationContextGlobalObject
    {
        internal UnifiedEvaluationContextGlobalObject (Agent agent) : base (agent)
        {
        }
    }
}