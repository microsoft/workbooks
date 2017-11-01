//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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