//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using AppKit;
using Foundation;
using CoreGraphics;

namespace Xamarin.Interactive.Client.Mac.CoordinateMappers
{
    sealed class InspectableWindow
    {
        static readonly NSString keyBounds = new NSString ("kCGWindowBounds");
        static readonly NSString keyWindowName = new NSString ("kCGWindowName");
        static readonly NSString keyWindowOwnerPid = new NSString ("kCGWindowOwnerPID");
        static readonly NSString keyWindowOwnerName = new NSString ("kCGWindowOwnerName");

        public static IEnumerable<InspectableWindow> GetWindowMatchingOwnerName (
            string nameFragment,
            bool onScreenOnly = true)
        {
            var windows = GetWindowList (onScreenOnly);
            if (windows == null || windows.Length == 0)
                yield break;

            foreach (var dict in windows) {
                string ownerName = dict [keyWindowOwnerName] as NSString;
                if (ownerName == null || !ownerName.Contains (nameFragment))
                    continue;

                CGRect bounds;
                if (!TryGetBounds (dict, out bounds))
                    continue;

                yield return new InspectableWindow {
                    Title = GetTitle (dict),
                    Bounds = bounds,
                };
            }
        }

        public static IEnumerable<InspectableWindow> GetWindows (string bundleIdentifier,
            bool onScreenOnly = true)
        {
            var apps = NSRunningApplication.GetRunningApplications (bundleIdentifier);
            if (apps == null)
                yield break;

            var windows = GetWindowList (onScreenOnly);
            if (windows == null || windows.Length == 0)
                yield break;

            foreach (var app in apps) {
                foreach (var dict in windows) {
                    var windowOwnerPid = dict [keyWindowOwnerPid] as NSNumber;
                    if ((windowOwnerPid?.Int32Value ?? -1) != app.ProcessIdentifier)
                        continue;

                    CGRect bounds;
                    if (!TryGetBounds (dict, out bounds))
                        continue;

                    yield return new InspectableWindow {
                        Application = app,
                        Title = GetTitle (dict),
                        Bounds = bounds
                    };
                }
            }
        }

        static NSDictionary [] GetWindowList (bool onScreenOnly)
            => CGWindowList.CopyWindowInfo (
                CGWindowListOptions.ExcludeDesktopElements |
                (onScreenOnly ? CGWindowListOptions.OnScreenOnly : 0),
                0);

        static bool TryGetBounds (NSDictionary cgWindowDict, out CGRect bounds)
        {
            bounds = CGRect.Empty;
            var boundsDict = cgWindowDict [keyBounds] as NSDictionary;
            return boundsDict != null && CGRect.TryParse (boundsDict, out bounds);
        }

        static string GetTitle (NSDictionary cgWindowDict)
            => cgWindowDict [keyWindowName] as NSString;

        public NSRunningApplication Application { get; private set; }
        public string Title { get; private set; }
        public CGRect Bounds { get; private set; }

        InspectableWindow ()
        {
        }

        public override string ToString ()
        {
            return String.Format ("PID: {0}, Title: {1}, Bounds: {2}",
                Application.ProcessIdentifier,
                Title,
                Bounds);
        }
    }
}