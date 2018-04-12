//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Unified;

namespace Xamarin.Interactive.iOS
{
    sealed class iOSAgent : Agent, IViewHierarchyHandler
    {
        string tmpDir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "tmp");

        public iOSAgent ()
        {
            Identity = new AgentIdentity (
                AgentType.iOS,
                Sdk.FromEntryAssembly ("iOS"),
                NSBundle.MainBundle.InfoDictionary ["CFBundleName"] as NSString,
                screenWidth: (int)UIScreen.MainScreen.Bounds.Width,
                screenHeight: (int)UIScreen.MainScreen.Bounds.Height);

            RepresentationManager.AddProvider (new iOSRepresentationProvider ());
            new UnifiedNativeHelper ().Initialize ();

            ViewHierarchyHandlerManager.AddViewHierarchyHandler ("UIKit", this);
        }

        protected override IdentifyAgentRequest GetIdentifyAgentRequest ()
            => IdentifyAgentRequest.FromCommandLineArguments (NSProcessInfo.ProcessInfo.Arguments);

        protected override EvaluationContextGlobalObject CreateEvaluationContextGlobalObject ()
            => new iOSEvaluationContextGlobalObject (this);

        protected override void HandleResetState ()
        {
            if (ClientSessionUri.SessionKind == ClientSessionKind.LiveInspection)
                return;

            var subviews = UIApplication.SharedApplication?.KeyWindow?.RootViewController?.View?.Subviews;
            if (subviews == null)
                return;

            foreach (var subview in subviews)
                subview.RemoveFromSuperview ();
        }

        internal override bool IncludePEImageInAssemblyDefinitions (HostOS compilationOS)
            => compilationOS != HostOS.macOS;

        internal override IEnumerable<string> GetReplDefaultUsingNamespaces ()
        {
            return base.GetReplDefaultUsingNamespaces ()
                .Concat (new [] { "Foundation", "CoreGraphics", "UIKit" });
        }

        public override void LoadExternalDependencies (
            Assembly loadedAssembly,
            IReadOnlyList<AssemblyDependency> externalDependencies)
        {
            if (externalDependencies == null)
                return;

            foreach (var externalDep in externalDependencies) {
                var location = externalDep.Location;

                if (externalDep.Data != null) {
                    try {
                        Directory.CreateDirectory (tmpDir);
                        location = Path.Combine (
                            tmpDir,
                            Path.GetFileName (externalDep.Location));
                        File.WriteAllBytes (location, externalDep.Data);
                    } catch {
                        continue;
                    }
                }

                Dlfcn.dlopen (location, 0);

                if (externalDep.Data != null)
                    File.Delete (location);
            }
        }

        bool IViewHierarchyHandler.TryGetRepresentedView (object view, bool withSubviews, out IInspectView representedView)
        {
            var uiview = view as UIView;
            if (uiview != null) {
                representedView = new iOSInspectView (uiview, withSubviews);
                return true;
            }

            representedView = null;
            return false;
        }

        public override InspectView GetVisualTree (string hierarchyKind)
            => ViewHierarchyHandlerManager.GetView (
                iOSEvaluationContextGlobalObject.KeyWindow,
                hierarchyKind);

        CALayer selectionLayer;

        public iOSInspectView HitTestHarder (UIView top, CGPoint point)
        {
            var layer = top.Layer.HitTest (point);
            var view = top.FindLayerView (layer);

            if (view == null)
                return null;

            if (view.Layer == layer)
                return new iOSInspectView (view, withSubviews: false);

            return new iOSInspectView (view, layer, visitedLayers: null, withSublayers: false);
        }

        bool IViewHierarchyHandler.TryGetHighlightedView (double x, double y, bool clear, out IInspectView highlightedView)
        {
            highlightedView = null;

            if (selectionLayer != null)
                selectionLayer.RemoveFromSuperLayer ();

            var top = UIApplication.SharedApplication.KeyWindow;
            var point = new CGPoint ((float)x, (float)y);
            var view = HitTestHarder (top, point);

            if (clear || view == null) {
                if (selectionLayer != null) {
                    selectionLayer.RemoveFromSuperLayer ();
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

        sealed class SelectionLayer : CALayer {
            public SelectionLayer ()
            {
                BackgroundColor = UIColor.Clear.CGColor;
                BorderColor = UIColor.Red.CGColor;
                BorderWidth = 1;
            }
        }

        public override InspectView HighlightView (double x, double y, bool clear, string hierarchyKind)
            => ViewHierarchyHandlerManager.HighlightView (x, y, clear, hierarchyKind);
    }
}