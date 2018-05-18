//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xamarin.Interactive.Client.Web.WebAssembly;
using Xamarin.Interactive.Core;

using IOFile = System.IO.File;

namespace Xamarin.Interactive.Client.Web.Controllers
{
    [Route ("/api/assembly")]
    public sealed class AssemblyController : Controller
    {
        readonly ReferenceWhitelist referenceWhitelist;

        public AssemblyController (ReferenceWhitelist referenceWhitelist)
            => this.referenceWhitelist = referenceWhitelist;

        [Route ("get")]
        public async Task<ActionResult> GetAssembly (string path)
        {
            path = Uri.UnescapeDataString (path);
            var filePath = new FilePath (path);

            if (!filePath.Exists || !referenceWhitelist.Contains (filePath))
                return NoContent ();

            return new FileStreamResult (IOFile.OpenRead (path), "application/octet-stream") {
                FileDownloadName = filePath.Name
            };
        }
    }
}