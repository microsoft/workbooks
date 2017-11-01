//
// SetLogLevelRequest.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class SetLogLevelRequest : MainThreadRequest<SuccessResponse>
	{
		public LogLevel LogLevel { get; set; }

		protected override Task<SuccessResponse> HandleAsync (Agent agent)
		{
			agent.SetLogLevel (LogLevel);
			return SuccessResponse.Task;
		}
	}
}
