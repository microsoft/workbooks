//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;

namespace Xamarin.Interactive
{
    [EditorBrowsable (EditorBrowsableState.Never)]
    static class InteractiveCulture
    {
        [EditorBrowsable (EditorBrowsableState.Never)]
        public static void Initialize ()
        {
            // WPF sets this in its app.config, but calling here does no harm
            AppContext.SetSwitch ("Switch.System.Globalization.NoAsyncCurrentCulture", true);
        }

        [ThreadStatic]
        static CultureInfo currentCulture;

        [EditorBrowsable (EditorBrowsableState.Never)]
        public static CultureInfo CurrentCulture {
            get { return currentCulture ?? Thread.CurrentThread.CurrentCulture; }
            set { Thread.CurrentThread.CurrentCulture = currentCulture = value; }
        }

        [ThreadStatic]
        static CultureInfo currentUICulture;

        [EditorBrowsable (EditorBrowsableState.Never)]
        public static CultureInfo CurrentUICulture {
            get { return currentUICulture ?? Thread.CurrentThread.CurrentUICulture; }
            set { Thread.CurrentThread.CurrentUICulture = currentUICulture = value; }
        }
    }
}