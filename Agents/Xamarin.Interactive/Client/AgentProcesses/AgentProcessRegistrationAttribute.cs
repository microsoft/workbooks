//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    [AttributeUsage (AttributeTargets.Assembly, AllowMultiple = true)]
    sealed class AgentProcessRegistrationAttribute : Attribute
    {
        public string WorkbookAppId { get; }
        public Type ProcessType { get; }

        public AgentProcessRegistrationAttribute (string workbookAppId, Type processType)
        {
            WorkbookAppId = workbookAppId;
            ProcessType = processType;
        }
    }
}
