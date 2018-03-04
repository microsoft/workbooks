using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Xamarin.Interactive.Client.Web.Middleware
{
    class WorkbooksAuthMiddleware
    {
        readonly RequestDelegate next;

        public WorkbooksAuthMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.User.Identity.IsAuthenticated) {
                string authnHeader = context.Request.Headers["Authorization"];
                if (string.IsNullOrWhiteSpace(authnHeader)) {
                    context.Response.StatusCode = 401;
                    context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Workbooks On The Web\"");
                    return;
                }

                const string basicScheme = "Basic ";
                if (!authnHeader.StartsWith(basicScheme, StringComparison.OrdinalIgnoreCase)) {
                    context.Response.StatusCode = 401;
                    context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Workbooks On The Web\"");
                    return;
                }

                var base64Creds = authnHeader.Substring(basicScheme.Length).Trim();
                var credentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Creds));
                var colon = credentials.IndexOf(':');
                var username = credentials.Substring(0, colon);
                var password = credentials.Substring(colon + 1);

                var expectedUsername = Environment.GetEnvironmentVariable("WORKBOOKS_WEB_USER");
                var expectedPassword = Environment.GetEnvironmentVariable("WORKBOOKS_WEB_PASS");
                if (username == expectedUsername && password == expectedPassword) {
                    var claims = new[] { new Claim(ClaimTypes.Upn, expectedUsername) };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.MaxValue,
                    });

                    await this.next(context);
                } else {
                    context.Response.StatusCode = 401;
                    context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Workbooks On The Web\"");
                    return;
                }
            } else {
                await this.next(context);
            }
        }
    }
}