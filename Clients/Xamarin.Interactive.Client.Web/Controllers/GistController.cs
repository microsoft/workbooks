//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Xamarin.Interactive.Client.Web.Controllers
{
    [Route ("/api/gist")]
    public sealed class GistController : Controller
    {
        [Route ("{id}")]
        public async Task<ActionResult> GetGistByIdAsync (string id)
        {
            using (var hc = new HttpClient ()) {
                hc.DefaultRequestHeaders.Add ("User-Agent", "Xamarin Workbooks");
                var gistApiResponse = await hc.GetAsync ($"https://api.github.com/gists/{id}");
                if (!gistApiResponse.IsSuccessStatusCode)
                    return StatusCode ((int)gistApiResponse.StatusCode);
                var gist = JObject.Parse (await gistApiResponse.Content.ReadAsStringAsync ());
                var latestGistRevision = (string)gist ["history"] [0] ["version"];
                var gistOwner = (string)gist ["owner"] ["login"];

                var gistZipUrl = $"https://gist.github.com/{gistOwner}/{id}/archive/{latestGistRevision}.zip";
                var gistZipResponse = await hc.GetAsync (gistZipUrl);
                if (!gistZipResponse.IsSuccessStatusCode)
                    return StatusCode ((int)gistZipResponse.StatusCode);

                return File (
                    await gistZipResponse.Content.ReadAsStreamAsync (),
                    "application/zip",
                    $"{latestGistRevision}.zip");
            }
        }
    }
}