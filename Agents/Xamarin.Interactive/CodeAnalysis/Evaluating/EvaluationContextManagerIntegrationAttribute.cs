//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive
{
    [AttributeUsage (AttributeTargets.Assembly)]
    public sealed class EvaluationContextManagerIntegrationAttribute : Attribute
    {
        public Type IntegrationType { get; }

        public EvaluationContextManagerIntegrationAttribute (Type integrationType)
            => IntegrationType = integrationType;
    }
}