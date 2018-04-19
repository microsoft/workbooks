//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using UIKit;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using Xamarin.Interactive;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.iOS;
using Xamarin.Interactive.Logging;

[assembly: EvaluationContextManagerIntegration (typeof (Xamarin.Interactive.Forms.iOS.iOSFormsAgentIntegration))]

namespace Xamarin.Interactive.Forms.iOS
{
    sealed class iOSFormsAgentIntegration : IEvaluationContextManagerIntegration
    {
        const string TAG = nameof (iOSFormsAgentIntegration);
        public const string HierarchyKind = "Xamarin.Forms";

        public void IntegrateWith (EvaluationContextManager evaluationContextManager)
        {
            var realAgent = evaluationContextManager.Context as iOSAgent;

            if (realAgent == null)
                return;

            if (realAgent.ViewHierarchyHandlerManager == null)
                return;

            try {
                realAgent.ViewHierarchyHandlerManager.AddViewHierarchyHandler (HierarchyKind,
                    new iOSFormsViewHierarchyHandler (realAgent));

                evaluationContextManager.RepresentationManager.AddProvider<FormsRepresentationProvider> ();

                if (realAgent.ClientSessionUri.SessionKind == ClientSessionKind.Workbook) {
                    evaluationContextManager.RegisterResetStateHandler (ResetStateHandler);

                    Log.Debug (TAG, "Initializing Xamarin.Forms.");
                    Xamarin.Forms.Forms.Init ();

                    Log.Debug (TAG, "Creating base Xamarin.Forms application.");
                    var app = new WorkbookApplication { MainPage = new ContentPage () };

                    Log.Debug (TAG, "Creating view controller for main page and setting it as root.");
                    UIApplication.SharedApplication.KeyWindow.RootViewController =
                        app.MainPage.CreateViewController ();
                }

                Log.Info (TAG, "Registered Xamarin.Forms agent integration!");
            } catch (Exception e) {
                Log.Error (TAG, "Could not register Xamarin.Forms agent integration.", e);
            }
        }

        void ResetStateHandler ()
        {
            // The RVC is our view controller, we just need to add our views back. :|
            var rvc = UIApplication.SharedApplication.KeyWindow.RootViewController;
            var renderer = Platform.GetRenderer (Application.Current.MainPage);
            var viewToBeAdded = renderer.NativeView;

            while (viewToBeAdded.Superview != null)
                viewToBeAdded = viewToBeAdded.Superview;

            rvc.View.AddSubview (viewToBeAdded);
        }
    }
}