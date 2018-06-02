// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Client.Web.Controllers
{
    [Route ("/api/identify")]
    public sealed class AgentIdentificationController : Controller
    {
        [HttpPost]
        public IActionResult Index (
            [FromQuery (Name = "token")] string token)
        {
            // Note: unfortunately cannot do '[FromBody] AgentIdentity' because
            // ASP.NET Core requires everything to be public for model binding,
            // and AgentIdentity is intentionally internal.
            var identity = (AgentIdentity)InteractiveJsonSerializerSettings
                .SharedInstance
                .CreateSerializer ()
                .Deserialize (Request.Body);

            if (ClientApp.SharedInstance.AgentIdentificationManager.RespondToAgentIdentityRequest (
                Guid.Parse (token),
                identity))
                return Ok ();

            return NotFound ();
        }
    }
}