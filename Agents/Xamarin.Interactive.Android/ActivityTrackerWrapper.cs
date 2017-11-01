//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

using Android.App;
using Android.Runtime;

namespace Xamarin.Interactive.Android
{
    interface IActivityTracker
    {
        IReadOnlyList<Activity> StartedActivities { get; }
    }

    // wrapper for internal Xamarin.Android class
    class ActivityTrackerWrapper : IActivityTracker
    {
        readonly Type trackerType;
        readonly object tracker;

        public ActivityTrackerWrapper ()
        {
            var monodroidDll = Assembly.GetAssembly (typeof(Application));
            trackerType = monodroidDll.GetType ("Android.App.ActivityTracker");
            tracker = Activator.CreateInstance (trackerType);

            // We cannot use the C# APIs to perform registration, because if there is a custom Application
            // subclass used for the app, accessing it at this time generates a MCW for the underlying Java
            // class, ultimately causing an infinite loop and stack overflow, since this is being called
            // during RegisterJniNatives.
            //
            // So, we use JNI to make sure the calls happen in Java land. This JNI code is equivalent to:
            //
            //	((Application)Application.Context).RegisterActivityLifecycleCallbacks (
            //		(Application.IActivityLifecycleCallbacks) tracker);

            var trackerJValue =
                new JValue (JNIEnv.ToJniHandle ((Application.IActivityLifecycleCallbacks)tracker));

            var klassMonoApp = JNIEnv.FindClass ("mono/MonoPackageManager");
            var klassAndroidApp = JNIEnv.FindClass ("android/app/Application");
            try {
                var contextFieldId = JNIEnv.GetStaticFieldID (
                    klassMonoApp, "Context", "Landroid/content/Context;");
                var contextLref = JNIEnv.GetStaticObjectField (klassMonoApp, contextFieldId);

                var registerMethodId = JNIEnv.GetMethodID (
                    klassAndroidApp,
                    "registerActivityLifecycleCallbacks",
                    "(Landroid/app/Application$ActivityLifecycleCallbacks;)V");

                JNIEnv.CallNonvirtualVoidMethod (
                    contextLref, klassAndroidApp, registerMethodId, trackerJValue);
            } finally {
                JNIEnv.DeleteGlobalRef (klassMonoApp);
                JNIEnv.DeleteGlobalRef (klassAndroidApp);
            }

            // Uncomment if we need access to ActivityStarted event:
            //var activityStartedEvent = trackerType.GetEvent ("ActivityStarted");
            //Action<object, EventArgs> handler = OnActivityStarted;
            //var d = Delegate.CreateDelegate (activityStartedEvent.EventHandlerType, this, "OnActivityStarted");
            //activityStartedEvent.AddEventHandler (tracker, d);
        }

        public IReadOnlyList<Activity> StartedActivities {
            get {
                return (IReadOnlyList<Activity>)
                    trackerType.GetProperty ("StartedActivities").GetValue (tracker);
            }
        }
    }
}

