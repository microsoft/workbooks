//
// MainActivity.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc.
// Copyright 2017 Microsoft. All rights reserved.

using System.Collections.Generic;

using Android.App;

using Xamarin.Interactive.Android;

namespace Xamarin.Workbooks.Android
{
	class ActivityTrackerStub : IActivityTracker
	{
		public ActivityTrackerStub (Activity activity)
		{
			StartedActivities = new List<Activity> { activity };
		}

		public IReadOnlyList<Activity> StartedActivities { get; }
	}
}