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
    public sealed class AgentIntegrationAttribute : Attribute
    {
        public Type AgentIntegrationType { get; }

        public AgentIntegrationAttribute (Type agentIntegrationType)
        {
            AgentIntegrationType = agentIntegrationType;
        }
    }
}