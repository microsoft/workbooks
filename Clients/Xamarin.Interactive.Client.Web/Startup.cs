//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xamarin.Interactive.Client.Web.Hosting;
using Xamarin.Interactive.Client.Web.Hubs;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Client.Web
{
    public sealed class Startup
    {
        public Startup (IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices (IServiceCollection services)
        {
            var searchPath = Path.Combine (
                Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location),
                "WorkbookApps");
            if (Directory.Exists (searchPath))
                WorkbookAppInstallation.RegisterSearchPath (searchPath);

            services.AddSingleton (new WebClientAppContainer ());

            services.AddMemoryCache ();
            services.AddMvc ();

            services
                .AddSignalR ()
                .AddJsonProtocol (options => {
                    options.PayloadSerializerSettings = new ExternalInteractiveJsonSerializerSettings ();
                });
        }

        public void Configure (
            IApplicationBuilder app,
            IHostingEnvironment env,
            IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
                app.UseWebpackDevMiddleware (new WebpackDevMiddlewareOptions {
                    HotModuleReplacement = true,
                    ReactHotModuleReplacement = true
                });
            } else {
                app.UseExceptionHandler ("/Home/Error");
            }

            app.UseMonacoMuting ();

            app.UseStaticFiles ();

            app.UseSignalR (routes => {
                routes.MapHub<InteractiveSessionHub> ("/session");
            });

            app.UseMvc (routes => {
                routes.MapRoute (
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute (
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}