//
// IAgentIntegration.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

namespace Xamarin.Interactive
{
	public interface IAgentIntegration
	{
		void IntegrateWith (IAgent agent);
	}
}