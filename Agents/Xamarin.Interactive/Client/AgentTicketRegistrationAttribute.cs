//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Client
{
    [AttributeUsage (AttributeTargets.Assembly, AllowMultiple = true)]
    sealed class AgentTicketRegistrationAttribute : Attribute
    {
        public string WorkbookAppId { get; }
        public Type TicketType { get; }

        public AgentTicketRegistrationAttribute (string workbookAppId, Type processType)
        {
            WorkbookAppId = workbookAppId;
            TicketType = processType;
        }
    }
}
