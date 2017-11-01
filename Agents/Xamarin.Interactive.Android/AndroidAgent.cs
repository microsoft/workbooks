//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

using Android.App;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Runtime;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Remote;

using AG = Android.Graphics;

namespace Xamarin.Interactive.Android
{
    class AndroidAgent : Agent, IViewHierarchyHandler
    {
        View highlightedView;
        Drawable highlightedViewOriginalBackground;

        AG.Point displaySize;
        int contentId;
        string deviceIpAddress;

        public IActivityTracker ActivityTracker { get; set; }
        internal int ContentId => contentId;

        public AndroidAgent (
            IActivityTracker activityTracker,
            int contentId = -1)
        {
            this.contentId = contentId;

            var windowManager = Application.Context.GetSystemService (
                global::Android.Content.Context.WindowService)
                .JavaCast<IWindowManager> ();
            displaySize = GetRealSize (windowManager.DefaultDisplay);

            ActivityTracker = activityTracker;

            Identity = new AgentIdentity (
                AgentType.Android,
                new Sdk (
                    new FrameworkName (typeof (Java.Interop.Runtime)
                        .Assembly
                        .GetCustomAttribute<TargetFrameworkAttribute> ()
                        .FrameworkName),
                    Array.Empty<string> (),
                    "Android"),
                GetApplicationName (),
                deviceManufacturer: Build.Manufacturer,
                screenWidth: displaySize.X,
                screenHeight: displaySize.Y);

            RepresentationManager.AddProvider (new AndroidRepresentationProvider ());

            ViewHierarchyHandlerManager.AddViewHierarchyHandler ("Android", this);
        }

        protected override IdentifyAgentRequest GetIdentifyAgentRequest ()
        {
            // Counterpart to AndroidAgentProcess.LaunchAppAsync (AmStartCommand)
            var agentIdentificationUri = ActivityTracker
                .StartedActivities
                .FirstOrDefault ()
                ?.Intent
                ?.Extras
                ?.GetString ("agentIdentificationUri");

            if (agentIdentificationUri != null &&
                Uri.TryCreate (agentIdentificationUri, UriKind.Absolute, out var uri))
                return new IdentifyAgentRequest (uri);

            return null;
        }

        protected override EvaluationContextGlobalObject CreateEvaluationContextGlobalObject ()
            => new AndroidEvaluationContextGlobalObject (this);

        protected override void HandleResetState ()
        {
            if (ClientSessionUri.SessionKind == ClientSessionKind.LiveInspection || contentId == -1)
                return;

            var activity = ActivityTracker?.StartedActivities?.FirstOrDefault ();
            activity?.FindViewById<ViewGroup> (contentId)?.RemoveAllViews ();
        }

        string GetApplicationName ()
        {
            var context = Application.Context;
            var packageManager = context.PackageManager;
            ApplicationInfo applicationInfo = null;
            try {
                applicationInfo = packageManager.GetApplicationInfo (context.ApplicationInfo.PackageName,
                    PackageInfoFlags.Configurations);
            } catch {
            }

            return (applicationInfo != null ? packageManager.GetApplicationLabel (applicationInfo) : "Unknown");
        }

        internal Activity GetTopActivity ()
        {
            return ActivityTracker?.StartedActivities?.LastOrDefault ();
        }

        bool IViewHierarchyHandler.TryGetRepresentedView (object view, bool withSubviews, out IInspectView representedView)
        {
            var androidView = view as View;
            if (androidView != null) {
                representedView = new AndroidInspectView (androidView, withSubviews);
                return true;
            }

            representedView = null;
            return false;
        }

        public override InspectView GetVisualTree (string hierarchyKind)
            => ViewHierarchyHandlerManager.GetView (
                GetTopActivity ()?.Window?.DecorView,
                hierarchyKind);

        public override InspectView HighlightView (double x, double y, bool clear, string hierarchyKind)
            => ViewHierarchyHandlerManager.HighlightView (
                x,
                y,
                clear,
                hierarchyKind);

        bool IViewHierarchyHandler.TryGetHighlightedView (double x, double y, bool clear, out IInspectView chosenView)
        {
            chosenView = null;

            if (highlightedView != null) {
                highlightedView.SetBackgroundDrawable (highlightedViewOriginalBackground);
                highlightedView = null;
                highlightedViewOriginalBackground = null;
            }

            var view = GetViewAt (GetTopActivity (), x, y);

            if (view == null)
                return false;

            if (!clear) {
                highlightedView = view;
                highlightedViewOriginalBackground = this.highlightedView.Background;

                var gd = new GradientDrawable ();
                gd.SetColor (AG.Color.Red.ToArgb ());
                gd.SetAlpha (255 / 2);
                //gd.SetCornerRadius (5);
                //gd.SetStroke (1, AG.Color.Red);

                Drawable highlightedBackground;
                if (highlightedViewOriginalBackground == null)
                    highlightedBackground = gd;
                else
                    highlightedBackground =
                        new LayerDrawable (new [] { highlightedViewOriginalBackground, gd });

                highlightedView.SetBackgroundDrawable (highlightedBackground);
            }

            chosenView = new AndroidInspectView (view, withSubviews: false);
            return true;
        }

        static View GetViewAt (Activity activity, double x, double y)
        {
            var rootLayout = activity?.Window?.DecorView?.RootView as ViewGroup;
            if (rootLayout == null)
                return null;

            return GetFrontmostChildAt (rootLayout, (int) x, (int) y);
        }

        static View GetFrontmostChildAt (ViewGroup viewGroup, int x, int y)
        {
            var locArray = new int[2];

            for (int i = viewGroup.ChildCount - 1; i >= 0; i--) {
                var child = viewGroup.GetChildAt (i);

                child.GetLocationOnScreen (locArray);
                var frame = new AG.Rect (
                    locArray [0],
                    locArray [1],
                    locArray [0] + child.Width,
                    locArray [1] + child.Height);

                if (!frame.Contains (x, y))
                    continue;

                var childGroup = child as ViewGroup;
                if (childGroup != null) {
                    var grandChild = GetFrontmostChildAt (childGroup, x, y);
                    if (grandChild != null)
                        return grandChild;
                }
                return child;
            }

            return null;
        }

        /// <summary>
        /// Equivalent of Display.GetRealSize (introduced in API 17), except this version works as far back as
        /// API 14.
        /// </summary>
        static AG.Point GetRealSize (Display display)
        {
            var realSize = new AG.Point ();

            var klassDisplay = JNIEnv.FindClass ("android/view/Display");
            var displayHandle = JNIEnv.ToJniHandle (display);
            try {
                // If the OS is running Jelly Bean (API 17), we can call Display.GetRealSize via JNI
                if ((int)Build.VERSION.SdkInt >= 17/*BuildVersionCodes.JellyBeanMr1*/) {
                    var getRealSizeMethodId = JNIEnv.GetMethodID (
                        klassDisplay,
                        "getRealSize",
                        "(Landroid/graphics/Point;)V");

                    JNIEnv.CallVoidMethod (
                        displayHandle, getRealSizeMethodId, new JValue (realSize));
                } else if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich) {
                    // Otherwise, this OS is older. As long as it's API 14-16, these private
                    // methods can get the real size.
                    var rawHeightMethodId = JNIEnv.GetMethodID (
                        klassDisplay,
                        "getRawHeight",
                        "()I");
                    var rawWidthMethodId = JNIEnv.GetMethodID (
                        klassDisplay,
                        "getRawWidth",
                        "()I");

                    var height = JNIEnv.CallIntMethod (displayHandle, rawHeightMethodId);
                    var width = JNIEnv.CallIntMethod (displayHandle, rawWidthMethodId);

                    realSize = new AG.Point (width, height);
                } else {
                    // Just return something for API < 14
                    display.GetSize (realSize);
                }
            } finally {
                JNIEnv.DeleteGlobalRef (klassDisplay);
            }

            return realSize;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
                ObjectCache.Shared.ClearHandles ();
            base.Dispose (disposing);
        }
    }
}
