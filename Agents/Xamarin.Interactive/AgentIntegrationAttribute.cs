//
// AgentIntegrationAttribute.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

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