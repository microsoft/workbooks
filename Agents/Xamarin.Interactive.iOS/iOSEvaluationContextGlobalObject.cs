//
// iOSEvaluationContextGlobalObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.

using System;

using UIKit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.iOS
{
	public sealed class iOSEvaluationContextGlobalObject : UnifiedEvaluationContextGlobalObject
	{
		internal iOSEvaluationContextGlobalObject (Agent agent) : base (agent)
		{
		}

		[InteractiveHelp (Description = "Quick access to UIApplication.SharedApplication.KeyWindow.RootViewController")]
		public static UIViewController RootViewController => UIApplication.SharedApplication.KeyWindow.RootViewController;

		[InteractiveHelp (Description = "Quick access to UIApplication.SharedApplication")]
		public static UIApplication App => UIApplication.SharedApplication;

		[InteractiveHelp (Description = "Quick access to UIApplication.SharedApplication.Delegate")]
		public static IUIApplicationDelegate AppDelegate => UIApplication.SharedApplication.Delegate;

		[InteractiveHelp (Description = "Quick access to UIApplication.SharedApplication.KeyWindow")]
		public static UIWindow KeyWindow => UIApplication.SharedApplication.KeyWindow;

		[InteractiveHelp (Description = "Get a screenshot of the given view")]
		public static UIImage Capture (UIView view, float? scale = null)
		{
			if (view == null)
				throw new ArgumentNullException (nameof(view));

			return ViewRenderer.Render (view.Window, view,
				scale == null ? UIScreen.MainScreen.Scale : scale.Value,
				skipChildren: false);
		}
	}
}