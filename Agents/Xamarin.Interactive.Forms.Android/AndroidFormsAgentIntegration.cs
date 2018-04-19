//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

using Android.Content;
using Android.Views;

using Xamarin.Forms.Platform.Android;

using Xamarin.Interactive;
using Xamarin.Interactive.Android;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Logging;

using Application = Android.App.Application;

[assembly: EvaluationContextManagerIntegration (typeof (Xamarin.Interactive.Forms.Android.AndroidFormsAgentIntegration))]

namespace Xamarin.Interactive.Forms.Android
{
    class AndroidFormsAgentIntegration : IEvaluationContextManagerIntegration
    {
        const string TAG = nameof (AndroidFormsAgentIntegration);

        // Keep this in sync with any changes to FormsActivity in the Android Workbook app project.
        // This name *must* be assembly-qualified for Type.GetType to work across the assembly boundary.
        const string FormsActivityTypeName = "Xamarin.Workbooks.Android.FormsActivity, Xamarin.Workbooks.Android";

        public const string HierarchyKind = "Xamarin.Forms";

        AndroidAgent realAgent;

        public void IntegrateWith (EvaluationContextManager evaluationContextManager)
        {
            realAgent = evaluationContextManager.Context as AndroidAgent;

            if (realAgent == null)
                return;

            if (realAgent.ViewHierarchyHandlerManager == null)
                return;

            try {
                realAgent.ViewHierarchyHandlerManager.AddViewHierarchyHandler (HierarchyKind,
                    new AndroidFormsViewHierarchyHandler (realAgent));

                evaluationContextManager.RepresentationManager.AddProvider<FormsRepresentationProvider> ();

                if (realAgent.ClientSessionUri.SessionKind == ClientSessionKind.Workbook) {
                    evaluationContextManager.RegisterResetStateHandler (ResetStateHandler);

                    // Set up launching the Forms activity.
                    Log.Debug (TAG, "Setting up activity type and grabbing current activity...");
                    var activityType = Type.GetType (FormsActivityTypeName);

                    if (activityType == null) {
                        Log.Warning (TAG, "Could not fully initialize Xamarin.Forms integration, missing Forms launch activity.");
                        return;
                    }

                    var intent = new Intent (Application.Context, activityType);
                    intent.AddFlags (ActivityFlags.NewTask);

                    var currentActivity = realAgent.GetTopActivity ();

                    // Launch the Forms activity.
                    Log.Debug (TAG, "Launching Forms activity via intent.");
                    Application.Context.StartActivity (intent);

                    // Wrap the previous activity up to reduce confusion.
                    Log.Debug (TAG, "Finishing existing activity.");
                    currentActivity.Finish ();
                }

                Log.Info (TAG, "Registered Xamarin.Forms agent integration!");
            } catch (Exception e) {
                Log.Error (TAG, "Could not register Xamarin.Forms agent integration.", e);
            }
        }

        void ResetStateHandler ()
        {
            var activity = realAgent.ActivityTracker?.StartedActivities?.FirstOrDefault ();
            var formsView = Xamarin.Forms.Application.Current.MainPage;
            var nativeViewGroup = Platform.GetRenderer (formsView).View;
            var parentToRestore = (ViewGroup) nativeViewGroup.Parent;

            while (parentToRestore.Parent != null)
                parentToRestore = (ViewGroup) parentToRestore.Parent;

            activity?.FindViewById<ViewGroup> (realAgent.ContentId)?.AddView (parentToRestore);
        }
    }
}
