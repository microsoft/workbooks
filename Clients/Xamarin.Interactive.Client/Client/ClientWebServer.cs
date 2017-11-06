//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Client
{
    /// <summary>
    /// Responsible for serving both internal resources (the web app portion of interactive,
    /// including all HTML/JS/CSS) and resources relative to a workbook document. This is
    /// how we enable support for displaying images with relative paths in Markdown in
    /// both IE and WebKit. Serving over HTTP instead of locally also works around a number
    /// of issues in IE. Only one instance should run in a given client process. It supports
    /// handling multiple open workbooks at once.
    /// </summary>
    sealed class ClientWebServer : HttpServer
    {
        public enum ResourceAction
        {
            /// <summary>
            /// The URI prefix is bound to the ClientWebServer but no resource
            /// could be resolved for it. Do nothing.
            /// </summary>
            Invalid,

            /// <summary>
            /// The main client web application should be served.
            /// </summary>
            InternalClientWebApp,

            /// <summary>
            /// The URI prefix is not bound to the ClientWebServer
            /// (e.g. http://microsoft.com). An appropriate action may be
            /// to open the URI in the default browser.
            /// </summary>
            ExternalResource,

            /// <summary>
            /// The resource represented by the URI is internal to the client.
            /// Do nothing outside of ClientWebServer itself!
            /// </summary>
            InternalResource,

            /// <summary>
            /// The resource represented by the URI is considered workbook
            /// content. Do the appropriate thing with the resolved path,
            /// such as opening the resolved path in its default application.
            /// </summary>
            WorkbookResource
        }

        const string TAG = nameof (ClientWebServer);

        const string workbookUriPrefix = "workbook/";
        const string staticUriPrefix = "static/";

        static readonly string [] nonClientPrefixes = {
            workbookUriPrefix,
            staticUriPrefix
        };

        readonly FilePath clientResourcesBundledBasePath;
        readonly FilePath clientResourcesSourceBasePath;

        public AgentIdentificationManager AgentIdentificationManager { get; }

        ImmutableDictionary<string, object> objects = ImmutableDictionary<string, object>.Empty;

        /// <summary>
        /// Base path for serving internal client resources from the built/bundled application.
        /// </summary>
        public FilePath BundledBasePath => clientResourcesBundledBasePath;

        /// <summary>
        /// Base path for serving internal client resources from the source directory/checkout.
        /// </summary>
        public FilePath SourceBasePath => clientResourcesSourceBasePath;

        public ClientWebServer (FilePath clientResourcesBundledBasePath)
        {
            this.clientResourcesBundledBasePath = clientResourcesBundledBasePath;
            this.clientResourcesSourceBasePath = DevEnvironment.RepositoryRootDirectory;

            if (clientResourcesSourceBasePath.DirectoryExists)
                clientResourcesSourceBasePath = clientResourcesSourceBasePath.Combine (
                    "Xamarin.Interactive.Client", "ClientApp");

            if (!clientResourcesBundledBasePath.IsNull && !clientResourcesBundledBasePath.DirectoryExists)
                throw new DirectoryNotFoundException (clientResourcesSourceBasePath);

            Start ();

            AgentIdentificationManager = new AgentIdentificationManager (
                new Uri (BaseUri, "/api/identify"));
        }

        // This is a real resource that's on disk somewhere—for example, web dependencies from a NuGet.
        public Uri AddStaticResource (string id, FilePath path)
        {
            if (string.IsNullOrWhiteSpace (id))
                throw new ArgumentNullException (nameof (id));

            objects = objects.Add (id, path);

            return new Uri (BaseUri, $"{staticUriPrefix}{id}");
        }

        public void RemoveStaticResource (string id)
        {
            if (string.IsNullOrWhiteSpace (id))
                throw new ArgumentNullException (nameof (id));

            objects = objects.Remove (id);
        }

        public Uri AddSession (ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException (nameof (clientSession));

            var sessionId = clientSession.Id.ToString ();

            objects = objects.Add (sessionId, clientSession);

            return new Uri (BaseUri, $"{workbookUriPrefix}{sessionId}/");
        }

        public void RemoveSession (ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException (nameof (clientSession));

            objects = objects.Remove (clientSession.Id.ToString ());
        }

        /// <summary>
        /// Maps a URI to either a resource path relative to the workbook indicated in
        /// the URI or to an internal resource bundled with the client application. If
        /// the return ResourceAction is anything other than Invalid or ExternalResource,
        /// then the localPath exists on disk.
        /// </summary>
        public ResourceAction TryGetLocalResourcePath (Uri uri, out FilePath localPath)
        {
            localPath = FilePath.Empty;

            if (uri.Host != BaseUri.Host || uri.Port != BaseUri.Port)
                return ResourceAction.ExternalResource;

            // strip any / or \ absolute path indicators so we can FilePath.Combine
            // to build a real absolute path relative to a workbook or the client
            var requestPath = uri?.LocalPath?.TrimStart (new [] { '/', '\\' });

            // if the path doesn't start with 'workbook/' or 'static/' it must be a client internal resource
            if (!nonClientPrefixes.Any (prefix => requestPath.StartsWith (prefix, StringComparison.Ordinal))) {
                #if DEBUG
                const string serveSourcePrefix = "serve-source/";
                if (clientResourcesSourceBasePath.DirectoryExists &&
                    requestPath.StartsWith (serveSourcePrefix, StringComparison.Ordinal)) {
                        requestPath = requestPath.Substring (serveSourcePrefix.Length);

                    localPath = clientResourcesSourceBasePath.Combine (requestPath);
                    if (localPath.FileExists)
                        return ResourceAction.InternalResource;
                }
                #endif

                localPath = clientResourcesBundledBasePath.Combine (requestPath);
                return localPath.FileExists
                    ? ResourceAction.InternalResource
                    : ResourceAction.Invalid;
            }

            if (requestPath.StartsWith (workbookUriPrefix, StringComparison.Ordinal))
                return TryHandleWorkbookResource (requestPath, out localPath);

            if (requestPath.StartsWith (staticUriPrefix, StringComparison.Ordinal))
                return TryHandleStaticResource (requestPath, out localPath);

            return ResourceAction.Invalid;
        }

        ResourceAction TryHandleStaticResource (string requestPath, out FilePath localPath)
        {
            localPath = default (FilePath);

            requestPath = requestPath.Substring (staticUriPrefix.Length);
            // In this case, the object ID is the entire name.
            string objectId = requestPath;

            object obj;
            if (!objects.TryGetValue (objectId, out obj))
                return ResourceAction.Invalid;

            if (obj is FilePath) {
                localPath = (FilePath)obj;
                return localPath.FileExists ? ResourceAction.WorkbookResource : ResourceAction.Invalid;
            }

            return ResourceAction.Invalid;
        }

        ResourceAction TryHandleWorkbookResource (string requestPath, out FilePath localPath)
        {
            localPath = default (FilePath);

            // strip off 'workbook/' prefix
            requestPath = requestPath.Substring (workbookUriPrefix.Length);

            var pathOffset = requestPath.IndexOf ('/');
            if (pathOffset <= 0)
                return ResourceAction.Invalid;

            string clientSessionId = requestPath.Substring (0, pathOffset);
            requestPath = requestPath.Substring (pathOffset + 1);

            object maybeClientSession;
            if (!objects.TryGetValue (clientSessionId, out maybeClientSession))
                return ResourceAction.Invalid;

            var session = maybeClientSession as ClientSession;

            if (session == null)
                return ResourceAction.Invalid;

            // if we have an empty relative request path, serve the actual client app
            if (String.IsNullOrEmpty (requestPath)) {
                localPath = clientResourcesBundledBasePath.Combine ("app.html");
                return ResourceAction.InternalClientWebApp;
            }

            // build the workbook-relative path
            if (!session.Workbook.WorkingBasePath.IsNull)
                localPath = session.Workbook.WorkingBasePath.Combine (requestPath);

            // physical workbook-relative resource
            if (localPath.FileExists)
                return ResourceAction.WorkbookResource;

            // virtual workbook-relative resource?
            Guid dependencyGuid;
            if (Guid.TryParse (localPath.NameWithoutExtension, out dependencyGuid) &&
                session.TryGetWebResource (dependencyGuid, out localPath) &&
                localPath.FileExists)
                return ResourceAction.WorkbookResource;

            return ResourceAction.Invalid;
        }

        protected override async Task PerformHttpAsync (
            HttpListenerContext context,
            CancellationToken cancellationToken)
        {
            Log.Debug (TAG, "Request: " + context.Request.Url);

            var urlPath = context.Request.Url.AbsolutePath;
            if (urlPath.StartsWith ("/api/", StringComparison.OrdinalIgnoreCase)) {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                try {
                    var apiRequestPath = urlPath
                        .Split (new [] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                        .Skip (1)
                        .ToArray ();
                    await RespondForApiRequestAsync (context, apiRequestPath, cancellationToken);
                } catch (Exception e) {
                    Log.Error (TAG, e, $"Exception thrown when handling API request for {urlPath}");
                }
                return;
            }

            switch (TryGetLocalResourcePath (context.Request.Url, out var localPath)) {
            case ResourceAction.Invalid:
                // Monaco has a custom AMD loader that tries to load modules via URL first,
                // which of course 404s, and spews a noisy mess to the JS console.
                // Simply ignore them.
                switch (context.Request.Url.LocalPath) {
                case "/monaco-editor/vs/platform/keybinding/common/keybindingResolver.js":
                case "/monaco-editor/vs/platform/keybinding/common/keybindingsRegistry.js":
                case "/monaco-editor/vs/platform/contextkey/common/contextkey.js":
                    context.Response.StatusCode = 204;
                    return;
                }

                await RespondWithResourceNotFoundAsync (context, localPath, cancellationToken);
                break;
            default:
                await RespondWithStaticResourceAsync (context, localPath, cancellationToken);
                break;
            }
        }

        Task RespondForApiRequestAsync (
            HttpListenerContext context,
            string [] apiRequestPath,
            CancellationToken cancellationToken)
        {
            if (apiRequestPath == null || apiRequestPath.Length == 0)
                return Task.CompletedTask;

            if (apiRequestPath [0] == "identify" && context.Request.HttpMethod == "POST") {
                if (AgentIdentificationManager.RespondToAgentIdentityRequest (
                    Guid.Parse (context.Request.QueryString ["token"]),
                    (AgentIdentity)new XipSerializer (
                    context.Request.InputStream,
                    InteractiveSerializerSettings.SharedInstance).Deserialize ()))
                    context.Response.StatusCode = 200;
            }

            return Task.CompletedTask;
        }

        async Task RespondWithStaticResourceAsync (
            HttpListenerContext context,
            FilePath localPath,
            CancellationToken cancellationToken)
        {
            switch (localPath.Extension) {
            case ".html":
                context.Response.ContentType = "text/html";
                context.Response.ContentEncoding = Utf8.Encoding;
                break;
            case ".css":
                context.Response.ContentType = "text/css";
                context.Response.ContentEncoding = Utf8.Encoding;
                break;
            case ".js":
                context.Response.ContentType = "application/javascript";
                context.Response.ContentEncoding = Utf8.Encoding;
                break;
            case ".png":
                context.Response.ContentType = "image/png";
                break;
            case ".jpg":
            case ".jpeg":
                context.Response.ContentType = "image/jpeg";
                break;
            case ".gif":
                context.Response.ContentType = "image/gif";
                break;
            case ".svg":
                context.Response.ContentType = "image/svg+xml";
                break;
            }

            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = localPath.FileSize;

            using (var resourceStream = localPath.OpenRead ())
                await resourceStream.CopyToAsync (
                    context.Response.OutputStream,
                    bufferSize: 81920,
                    cancellationToken: cancellationToken);
        }

        async Task RespondWithResourceNotFoundAsync (
            HttpListenerContext context,
            FilePath localPath,
            CancellationToken cancellationToken)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "text/html";
            context.Response.ContentEncoding = Utf8.Encoding;

            var writer = new StreamWriter (context.Response.OutputStream);

            var message = localPath.IsNull
                ? "Invalid Workbook"
                : "Invalid Workbook Resource";

            writer.WriteLine ("<!DOCTYPE html>");
            writer.WriteLine ("<html>");
            writer.WriteLine ("  <head>");
            writer.WriteLine ("    <meta charset=\"utf-8\">");
            writer.WriteLine ("    <meta http-equiv=\"content-type\" content=\"text/html; charset=utf-8\">");
            writer.WriteLine ("    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">");
            writer.WriteLine ("    <title>Four-oh-Four Not Found</title>");
            writer.WriteLine ("    <style type=\"text/css\">");
            writer.WriteLine ("      body { ");
            writer.WriteLine ("        font-family: sans-serif;");
            writer.WriteLine ("        font-size: 1em;");
            writer.WriteLine ("        color: rgb(255,255,255);");
            writer.WriteLine ("        background-color: rgb(167,72,169);");
            writer.WriteLine ("        text-align: center;");
            writer.WriteLine ("      }");
            writer.WriteLine ("    </style>");
            writer.WriteLine ("  </head>");
            writer.WriteLine ("  <body>");
            writer.WriteLine ($"    <h1>🙉 {message} 🙈</h1>");

            if (!localPath.IsNull)
                writer.WriteLine ($"    <p><code>{localPath}</code></p>");

            writer.WriteLine ("  </body>");
            writer.WriteLine ("</html>");

            var junk = new byte [512];
            new Random ().NextBytes (junk);
            writer.WriteLine ("<!-- here you go, IE... what... I just... sigh.");
            writer.WriteLine (Convert.ToBase64String (junk));
            writer.WriteLine ("-->");

            await writer.FlushAsync ();
        }
    }
}