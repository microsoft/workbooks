//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;

namespace Xamarin.Interactive.Client.Web.Controllers
{
    [Route ("api/[controller]")]
    public sealed class SessionController : Controller
    {
        [Route ("create")]
        public IActionResult Create ()
            => Ok ();
    }
}