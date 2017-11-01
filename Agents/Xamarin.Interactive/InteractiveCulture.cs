//
// InteractiveCulture.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;

namespace Xamarin.Interactive
{
    [EditorBrowsable (EditorBrowsableState.Never)]
    public static class InteractiveCulture
    {
        [EditorBrowsable (EditorBrowsableState.Never)]
        public static void Initialize ()
        {
#if !WPF
            AppContext.SetSwitch ("Switch.System.Globalization.NoAsyncCurrentCulture", true);
#endif
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