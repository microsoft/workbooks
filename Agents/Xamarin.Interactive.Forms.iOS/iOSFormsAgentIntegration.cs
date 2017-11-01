//
// AgentIntegration.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using UIKit;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using Xamarin.Interactive;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.iOS;
using Xamarin.Interactive.Logging;

[assembly: AgentIntegration (typeof (Xamarin.Interactive.Forms.iOS.iOSFormsAgentIntegration))]

namespace Xamarin.Interactive.Forms.iOS
{
    class iOSFormsAgentIntegration : IAgentIntegration
    {
        const string TAG = nameof (iOSFormsAgentIntegration);
        public const string HierarchyKind = "Xamarin.Forms";

        iOSAgent realAgent;

        public void IntegrateWith (IAgent agent)
        {
            // TODO: Refactor this into shared code between iOS and Android.
            realAgent = agent as iOSAgent;

            if (realAgent == null)
                return;

            if (realAgent.ViewHierarchyHandlerManager == null)
                return;

            try {
                realAgent.ViewHierarchyHandlerManager.AddViewHierarchyHandler (HierarchyKind,
                    new iOSFormsViewHierarchyHandler (realAgent));
                realAgent.RepresentationManager.AddProvider (new FormsRepresentationProvider ());

                if (realAgent.ClientSessionUri.SessionKind == ClientSessionKind.Workbook) {
                    realAgent.RegisterResetStateHandler (ResetStateHandler);

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

        private void ResetStateHandler ()
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
