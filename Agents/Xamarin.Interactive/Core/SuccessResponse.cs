//
// SuccessResponse.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class SuccessResponse : IObjectReference
	{
		static readonly SuccessResponse singleton = new SuccessResponse ();

		public static readonly Task<SuccessResponse> Task
			= System.Threading.Tasks.Task.FromResult (singleton);

		SuccessResponse ()
		{
		}

		object IObjectReference.GetRealObject (StreamingContext context) => singleton;
	}
}