//
// UnifiedEvaluationContextGlobalObject.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.
// Copyright 2017 Microsoft. All rights reserved.

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