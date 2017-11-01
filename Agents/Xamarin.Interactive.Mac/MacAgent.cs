//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using ObjCRuntime;
using Foundation;
using AppKit;
using CoreAnimation;
using CoreGraphics;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Mac
{
    sealed class MacAgent : Agent, IViewHierarchyHandler
    {
        const string TAG = nameof (MacAgent);

        NSWindow workbookMainWindow;

        [DllImport (Constants.SystemConfigurationLibrary)]
        static extern IntPtr SCDynamicStoreCopyComputerName (IntPtr store, IntPtr nameEncoding);

        public MacAgent ()
        {
            #if MAC_MOBILE
			var agentType = AgentType.MacMobile;
			const string profile = "Modern";
            #elif MAC_DESKTOP
			var agentType = AgentType.MacNet45;
			const string profile = "Full";
            #endif

            Identity = new AgentIdentity (
                agentType,
                Sdk.FromEntryAssembly ("Xamarin.Mac", profile),
                NSBundle.MainBundle.InfoDictionary ["CFBundleName"] as NSString);

            RepresentationManager.AddProvider (new MacRepresentationProvider ());

            ViewHierarchyHandlerManager.AddViewHierarchyHandler ("AppKit", this);
        }

        protected override IdentifyAgentRequest GetIdentifyAgentRequest ()
            => IdentifyAgentRequest.FromCommandLineArguments (NSProcessInfo.ProcessInfo.Arguments);

        protected override EvaluationContextGlobalObject CreateEvaluationContextGlobalObject ()
            => new MacEvaluationContextGlobalObject (this);

        protected override void HandleResetState ()
        {
            if (ClientSessionUri.SessionKind == ClientSessionKind.LiveInspection)
                return;

            foreach (var window in NSApplication.SharedApplication.Windows) {
                window.ResignKeyWindow ();
                window.ResignMainWindow ();
                window.Close ();
            }

            workbookMainWindow?.Dispose ();
            workbookMainWindow = null;
        }

        internal override IEnumerable<string> GetReplDefaultUsingNamespaces ()
        {
            return base.GetReplDefaultUsingNamespaces ()
                .Concat (new [] { "Foundation", "CoreGraphics", "AppKit" });
        }

        public NSWindow GetMainWindow ()
        {
            if (ClientSessionUri.SessionKind == ClientSessionKind.LiveInspection)
                return NSApplication.SharedApplication.MainWindow ??
                    NSApplication.SharedApplication.Windows?.FirstOrDefault ();

            if (workbookMainWindow == null) {
                string xmDisplayName = null;
                switch (Identity.AgentType) {
                case AgentType.MacNet45:
                    xmDisplayName = "Full Profile";
                    break;
                case AgentType.MacMobile:
                    xmDisplayName = "Modern Profile";
                    break;
                default:
                    throw new NotImplementedException ($"AgentType.{Identity.AgentType}");
                }

                workbookMainWindow = new NSWindow {
                    Title = $"Workbook Main Window - {xmDisplayName}",
                    StyleMask = NSWindowStyle.Resizable |
                        NSWindowStyle.Closable |
                        NSWindowStyle.Miniaturizable |
                        NSWindowStyle.Titled
                };

                workbookMainWindow.SetContentSize (new CGSize (500, 400));
            }

            workbookMainWindow.BecomeMainWindow ();
            workbookMainWindow.MakeKeyAndOrderFront (null);

            return workbookMainWindow;
        }

        bool IViewHierarchyHandler.TryGetRepresentedView (object view, bool withSubviews, out IInspectView representedView)
        {
            // TODO: Add withSubviews support to MacInspectView.
            if (view is NSApplication) {
                representedView = new MacRootInspectView {
                    DisplayName = Identity.ApplicationName
                };
                return true;
            }

            var nsview = view as NSView;
            if (nsview != null) {
                representedView = new MacInspectView (nsview);
                return true;
            }

            representedView = null;
            return false;
        }

        public override InspectView GetVisualTree (string hierarchyKind)
            => ViewHierarchyHandlerManager.GetView (
                NSApplication.SharedApplication,
                hierarchyKind);

        NSView selectionLayer;

        MacInspectView HitTestHarder (NSView root, CGPoint point)
        {
            var view = root.HitTest (point);
            if (view != null)
                return new MacInspectView (view);

            foreach (var sub in root.TraverseTree (v => v.Subviews)) {
                var layer = sub?.Layer?.HitTest (point);

                if (layer != null)
                    return new MacInspectView (sub, layer, visitedLayers: null);
            }

            return null;
        }

        bool IViewHierarchyHandler.TryGetHighlightedView (double x, double y, bool clear, out IInspectView highlightedView)
        {
            highlightedView = null;

            MacInspectView view = null;
            foreach (var window in NSApplication.SharedApplication.Windows) {
                var position = window.ConvertRectFromScreen (new CGRect (x, y, 1, 1)).Location;
                position = window.ContentView.ConvertPointFromView (position, null);
                view = HitTestHarder (window.ContentView, position);
                if (view != null)
                    break;
            }

            if (clear || view == null) {
                if (selectionLayer != null) {
                    selectionLayer.RemoveFromSuperview ();
                    selectionLayer.Dispose ();
                    selectionLayer = null;
                }

                if (view == null)
                    return false;

                highlightedView = view;
                return true;
            }

            selectionLayer = view.UpdateSelection (selectionLayer ?? new SelectionLayer ());
            highlightedView = view;
            return true;
        }

        public override InspectView HighlightView (double x, double y, bool clear, string hierarchyKind)
            => ViewHierarchyHandlerManager.HighlightView (x, y, clear, hierarchyKind);

        sealed class SelectionLayer : NSView
        {
            public CALayer DisplayLayer { get; }

            public SelectionLayer ()
            {
                var layer = new CALayer {
                    BackgroundColor = NSColor.Clear.CGColor,
                    BorderColor = NSColor.Red.CGColor,
                    BorderWidth = 2
                };

                var viewLayer = new CALayer ();
                viewLayer.AddSublayer (layer);
                Layer = viewLayer;
                WantsLayer = true;
            }

            public override void Layout ()
            {
                base.Layout ();
                Frame = Superview.Bounds;
            }

            public override NSView HitTest (CGPoint aPoint)
            {
                return null;
            }
        }

        public override void LoadExternalDependencies (
            Assembly loadedAssembly,
            AssemblyDependency [] externalDependencies)
        {
            if (externalDependencies == null)
                return;

            // We can't do anything until we've loaded the assembly, because we need
            // to insert things specifically into its DllMap.
            if (loadedAssembly == null)
                return;

            foreach (var externalDep in externalDependencies) {
                try {
                    Log.Debug (TAG, $"Loading external dependency from {externalDep.Location}…");
                    MonoSupport.AddDllMapEntries (loadedAssembly, externalDep);
                } catch (Exception e) {
                    Log.Error (TAG, "Could not load external dependency.", e);
                }
            }
        }
    }
}