//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

namespace Xamarin.Interactive.Client.Web.Controllers
{
    [Route ("api/[controller]")]
    public sealed class SampleDataController : Controller
    {
        static string [] Summaries = {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet ("[action]")]
        public IEnumerable<WeatherForecast> WeatherForecasts ()
        {
            var rng = new Random ();
            return Enumerable.Range (1, 5).Select (index => new WeatherForecast {
                DateFormatted = DateTime.Now.AddDays (index).ToString ("d"),
                TemperatureC = rng.Next (-20, 55),
                Summary = Summaries [rng.Next (Summaries.Length)]
            });
        }

        public sealed class WeatherForecast
        {
            public string DateFormatted { get; set; }
            public int TemperatureC { get; set; }
            public string Summary { get; set; }

            public int TemperatureF
                => 32 + (int)(TemperatureC / 0.5556);
        }
    }
}