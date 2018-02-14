//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Xamarin.Interactive.Client.Web
{
    public class Program
    {
        public static void Main (string [] args)
            => BuildWebHost (args).Run ();

        public static IWebHost BuildWebHost (string [] args) =>
            WebHost.CreateDefaultBuilder (args)
                .UseStartup<Startup> ()
                .Build ();
    }
}