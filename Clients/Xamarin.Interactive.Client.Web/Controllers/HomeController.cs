//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

namespace Xamarin.Interactive.Client.Web.Controllers
{
    public sealed class HomeController : Controller
    {
        public IActionResult Index ()
            => View ();

        public IActionResult Wasm ()
            => View ();

        public IActionResult Error ()
        {
            ViewData ["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View ();
        }
    }
}