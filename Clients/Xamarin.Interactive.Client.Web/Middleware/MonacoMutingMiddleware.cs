//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Xamarin.Interactive.Client.Web
{
    public static class MonacoMutingMiddlewareExtensions
    {
        /// <summary>
        /// Monaco has a custom AMD loader that tries to load modules via URL first,
        /// which of course 404s, and spews a noisy mess to the JS console.
        /// Simply ignore them by return 204 - they will have already been loaded
        /// via Monaco's own internal mechanisms.
        /// </summary>
        public static IApplicationBuilder UseMonacoMuting (this IApplicationBuilder builder)
            => builder.UseMiddleware<Middlewear.MonacoMutingMiddleware> ();
    }

    namespace Middlewear
    {
        sealed class MonacoMutingMiddleware
        {
            readonly RequestDelegate next;

            public MonacoMutingMiddleware (RequestDelegate next)
                => this.next = next;

            public Task Invoke (HttpContext context)
            {
                switch (context.Request.Path) {
                // Keep in sync with ClientApp/utils/MonacoLoader.ts
                case "/vs/platform/keybinding/common/keybindingsRegistry.js":
                case "/vs/platform/contextkey/common/contextkey.js":
                    context.Response.StatusCode = 204;
                    return Task.CompletedTask;
                default:
                    return next (context);
                }
            }
        }
    }
}