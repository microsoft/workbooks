using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Xamarin.Interactive.Client.Web
{
    public static class WasmStaticFileMiddleware
    {
        public static IApplicationBuilder UseWasmStaticFiles (this IApplicationBuilder builder)
        {
            const string wasmWorkbookAppId = "webassembly-monowebassembly";
            var wasmWorkbookApp = WorkbookAppInstallation.LookupById (wasmWorkbookAppId);

            if (wasmWorkbookApp != null) {
                var provider = new FileExtensionContentTypeProvider ();
                provider.Mappings.Add (".wasm", "application/wasm");

                builder.UseStaticFiles (new StaticFileOptions {
                    FileProvider = new PhysicalFileProvider (wasmWorkbookApp.AppPath),
                    RequestPath = "/wasm",
                    ContentTypeProvider = provider,
                    ServeUnknownFileTypes = true
                });
            } else {
                Logging.Log.Warning(
                    nameof (WasmStaticFileMiddleware),
                    "Could not find WASM workbook app to register static file serving.");
            }

            return builder;
        }
    }
}