//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Client.Web
{
    static class IServiceProviderExtensions
    {
        public static T GetService<T> (this IServiceProvider serviceProvider) where T : class
            => serviceProvider.GetService (typeof (T)) as T;

        public static InteractiveSessionHubManager GetInteractiveSessionHubManager (
            this IServiceProvider serviceProvider)
            => serviceProvider.GetService<InteractiveSessionHubManager> ();
    }
}