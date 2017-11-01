//
// InspectorSupport.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using System.Threading;

using Android.App;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Android;

namespace Xamarin
{
	public static partial class InspectorSupport
	{
		static readonly SynchronizationContext mainContext = Application.SynchronizationContext;
		static Timer timer;
		static readonly ActivityTrackerWrapper activityTracker = new ActivityTrackerWrapper ();

		static partial void CreateAgent (AgentStartOptions startOptions)
		{
			mainContext.Post (
				s => {
					timer.Dispose ();
					agent = new AndroidAgent (activityTracker).Start (startOptions);
				},
				null);
		}

		internal static void StartBreakdance ()
		{
			timer = new Timer (
				s => {
					timer.Change (Timeout.Infinite, Timeout.Infinite);
					mainContext.Post (st => {
						BreakdanceStep ();
						timer.Change (1000, Timeout.Infinite);
					}, null);
				},
				null,
				1000,
				Timeout.Infinite);
		}
	}
}
