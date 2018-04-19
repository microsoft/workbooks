//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Wpf
{
    sealed class WpfAgent : Agent, IViewHierarchyHandler
    {
        const string TAG = nameof (WpfAgent);

        readonly Func<Window> mainWindowCreator;

        Window latestMainWindow;
        readonly WpfRepresentationProvider wpfRepresentationProvider;

        public WpfAgent (Func<Window> mainWindowCreator = null)
        {
            this.mainWindowCreator = mainWindowCreator;
            latestMainWindow = mainWindowCreator?.Invoke ();
            if (latestMainWindow != null) {
                latestMainWindow.WindowState = WindowState.Minimized;
                latestMainWindow.Show ();
            }

            Identity = new AgentIdentity (
                AgentType.WPF,
                Sdk.FromEntryAssembly ("WPF"),
                Assembly.GetEntryAssembly ().GetName ().Name);

            RepresentationManager.AddProvider (wpfRepresentationProvider = new WpfRepresentationProvider ());

            ViewHierarchyHandlerManager.AddViewHierarchyHandler ("WPF", this);
        }

        protected override IdentifyAgentRequest GetIdentifyAgentRequest ()
            => IdentifyAgentRequest.FromCommandLineArguments (Environment.GetCommandLineArgs ());

        protected override EvaluationContextManager CreateEvaluationContextManager ()
            => new WpfEvaluationContextManager (this);

        sealed class WpfEvaluationContextManager : EvaluationContextManager
        {
            readonly WpfAgent agent;

            public WpfEvaluationContextManager (WpfAgent agent)
                : base (agent.RepresentationManager, agent)
                => this.agent = agent;

            static readonly string [] defaultImports = {
                "System.Windows",
                "System.Windows.Controls",
                "System.Windows.Media"
            };

            protected override TargetCompilationConfiguration PrepareTargetCompilationConfiguration (
                TargetCompilationConfiguration configuration)
                => configuration.With (
                    defaultImports: configuration.DefaultImports.Concat (defaultImports).ToArray ());

            protected override object CreateGlobalState ()
                => new WpfEvaluationContextGlobalObject (agent);

            protected override void OnResetState ()
                => agent.ResetState ();

            internal override void LoadExternalDependencies (
                Assembly loadedAssembly,
                IReadOnlyList<AssemblyDependency> externalDependencies)
            {
                if (externalDependencies == null)
                    return;

                foreach (var externalDep in externalDependencies) {
                    try {
                        Log.Debug (TAG, $"Loading external dependency from {externalDep.Location}…");
                        WindowsSupport.LoadLibrary (externalDep.Location);
                    } catch (Exception e) {
                        Log.Error (TAG, "Could not load external dependency.", e);
                    }
                }
            }
        }

        void ResetState ()
        {
            if (ClientSessionUri.SessionKind == Client.ClientSessionKind.LiveInspection ||
                mainWindowCreator == null)
                return;

            var mainWindow = mainWindowCreator ();
            if (latestMainWindow != null) {
                try {
                    mainWindow.WindowState = latestMainWindow.WindowState;
                } catch {
                }
            }
            latestMainWindow = mainWindow;

            mainWindow.Show ();
            foreach (Window window in Application.Current.Windows)
                if (window != mainWindow)
                    window.Close ();
        }

        bool IViewHierarchyHandler.TryGetRepresentedView (object view, bool withSubviews, out IInspectView representedView)
        {
            if (view is Application) {
                representedView = new WpfRootInspectView { DisplayName = Identity.ApplicationName };
                return true;
            }

            var frameworkElement = view as FrameworkElement;
            if (frameworkElement != null) {
                representedView = new WpfInspectView (frameworkElement, withSubviews);
                return true;
            }

            representedView = null;
            return false;
        }

        public override InspectView GetVisualTree (string hierarchyKind)
            => ViewHierarchyHandlerManager.GetView (
                Application.Current,
                hierarchyKind);

        public override InspectView HighlightView (double x, double y, bool clear, string hierarchyKind)
            => ViewHierarchyHandlerManager.HighlightView (
                x,
                y,
                clear,
                hierarchyKind);

        protected override void Dispose (bool disposing)
        {
            if (disposing)
                ObjectCache.Shared.ClearHandles ();
            base.Dispose (disposing);
        }

        bool IViewHierarchyHandler.TryGetHighlightedView (double x, double y, bool clear, out IInspectView highlightedView)
        {
            highlightedView = null;

            var view = Mouse.DirectlyOver as FrameworkElement;

            if (view == null)
                return false;

            highlightedView = new WpfInspectView (view, withSubviews: false);
            return true;
        }
    }
}