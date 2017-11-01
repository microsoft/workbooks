//
// Author:
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Graphics;

using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Android
{
    public sealed class AndroidEvaluationContextGlobalObject : EvaluationContextGlobalObject
    {
        readonly AndroidAgent agent;

        internal AndroidEvaluationContextGlobalObject (AndroidAgent agent) : base (agent)
            => this.agent = agent;

        [InteractiveHelp (Description = "Return a screenshot of the given view")]
        public static Bitmap Capture (View view)
        {
            if (view == null)
                throw new ArgumentNullException (nameof(view));

            return ViewRenderer.Render (view, skipChildren: false);
        }

        [InteractiveHelp (Description = "All known Activities for the current app")]
        public IReadOnlyList<Activity> StartedActivities =>
            agent.ActivityTracker?.StartedActivities ?? new List<Activity> ();
    }
}