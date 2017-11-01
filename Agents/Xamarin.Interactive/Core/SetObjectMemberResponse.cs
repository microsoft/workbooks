//
// SetObjectMemberResponse.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class SetObjectMemberResponse
	{
		public bool Success { get; set; }
		public InteractiveObject UpdatedValue { get; set; }
	}
}