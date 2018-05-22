//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Client.AgentProcesses;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive
{
    sealed class WorkbookAppInstallation : IWorkbookAppInstallation
    {
        const string TAG = nameof (WorkbookAppInstallation);

        static readonly Assembly [] processManagerAssemblies = {
            Assembly.GetEntryAssembly (),
            typeof (WorkbookAppInstallation).Assembly
        };

        static readonly Assembly [] ticketProvidersAssemblies = processManagerAssemblies;

        static bool registeredDefaultProcessManagers;
        static bool registeredDefaultTicketProviders;

        static HashSet<AgentProcessRegistrationAttribute> processManagers;
        static HashSet<AgentTicketRegistrationAttribute> ticketProviders;

        public static void RegisterTicketProviders (Assembly assembly)
        {
            if (ticketProviders == null)
                ticketProviders = new HashSet<AgentTicketRegistrationAttribute> ();

            foreach (var attribute in assembly
                .GetCustomAttributes (typeof (AgentTicketRegistrationAttribute), false)
                .Cast<AgentTicketRegistrationAttribute> ())
                ticketProviders.Add (attribute);
        }

        static void RegisterDefaultTicketProviders ()
        {
            if (!registeredDefaultTicketProviders) {
                registeredDefaultTicketProviders = true;
                foreach (var assembly in ticketProvidersAssemblies)
                    RegisterTicketProviders (assembly);
            }
        }

        public static void RegisterProcessManagers (Assembly assembly)
        {
            if (processManagers == null)
                processManagers = new HashSet<AgentProcessRegistrationAttribute> ();

            foreach (var attribute in assembly
                .GetCustomAttributes (typeof (AgentProcessRegistrationAttribute), false)
                .Cast<AgentProcessRegistrationAttribute> ())
                processManagers.Add (attribute);
        }

        static void RegisterDefaultProcessManagers ()
        {
            if (!registeredDefaultProcessManagers) {
                registeredDefaultProcessManagers = true;
                foreach (var assemby in processManagerAssemblies)
                    RegisterProcessManagers (assemby);
            }
        }

        static readonly List<Func<string, string>> pathMappers = new List<Func<string, string>> {
            path => path.Replace ("{rid}", Runtime.CurrentProcessRuntime.RuntimeIdentifier),
        };

        static readonly List<string> searchPaths = new List<string> ();

        public static IReadOnlyList<WorkbookAppInstallation> All => all.Value;
        static readonly Lazy<IReadOnlyList<WorkbookAppInstallation>> all
            = new Lazy<IReadOnlyList<WorkbookAppInstallation>> (LocateWorkbookApps);

        public static void RegisterSearchPath (string searchPath)
        {
            if (!Directory.Exists (searchPath))
                throw new DirectoryNotFoundException (searchPath);

            searchPaths.Add (searchPath);
        }

        public static void RegisterPathMapper (Func<string, string> pathMapper)
            => pathMappers.Add (pathMapper);

        public static WorkbookAppInstallation LookupById (string workbookAppId)
            => All.FirstOrDefault (app => string.Equals (
                app.Id, workbookAppId, StringComparison.OrdinalIgnoreCase));

        #pragma warning disable 0618

        public static WorkbookAppInstallation Locate (AgentType agentType)
            => LookupById (AgentIdentity.GetFlavorId (agentType));

        public AgentType GetAgentType ()
            => AgentIdentity.GetAgentType (Id);

        #pragma warning restore 0618

        readonly IAgentProcessManager processManager;
        readonly Type ticketType;

        public string Id { get; }

        /// <summary>
        /// The "flavor" is the top-level grouping of workbook apps.
        /// The short form would be "iOS", for example.
        /// </summary>
        public string Flavor { get; }

        /// <summary>
        /// Icon name.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// IDs of any optional features the workbook app supports.
        /// </summary>
        public string [] OptionalFeatures {get; }

        /// <summary>
        /// SDK information for this application.
        /// </summary>
        public Sdk Sdk { get; }

        /// <summary>
        /// Path to the actual application.
        /// </summary>
        public string AppPath { get; }

        readonly int order;

        WorkbookAppInstallation (
            string id,
            string flavor,
            string icon,
            string [] optionalFeatures,
            Sdk sdk,
            string appPath,
            string appManagerAssembly,
            string ticketProviderAssembly,
            int order)
        {
            Id = id ?? throw new ArgumentNullException (nameof (id));

            Flavor = flavor
                ?? throw new ArgumentNullException (nameof (flavor));

            Icon = icon;

            OptionalFeatures = optionalFeatures ?? Array.Empty<string> ();

            Sdk = sdk
                ?? throw new ArgumentNullException (nameof (sdk));

            AppPath = appPath
                ?? throw new ArgumentNullException (nameof (appPath));

            this.order = order;

            if (appManagerAssembly == null)
                RegisterDefaultProcessManagers ();
            else
                RegisterProcessManagers (Assembly.LoadFrom (appManagerAssembly));

            if (ticketProviderAssembly == null)
                RegisterDefaultTicketProviders ();
            else
                RegisterTicketProviders (Assembly.LoadFrom (ticketProviderAssembly));

            var processType = processManagers
                .FirstOrDefault (r => r.WorkbookAppId == Id)
                ?.ProcessType;

            if (processType == null)
                throw new Exception (
                    $"No {nameof (IAgentProcess)} registered for workbook app ID '{Id}'");

            try {
                processManager = new AgentProcessManager (this, processType);
            } catch (Exception e) {
                throw new Exception (
                    $"Unable to instantiate AgentProcessManager with {processType.FullName} " +
                        $"for workbook app ID '{Id}'",
                    e);
            }

            ticketType = ticketProviders
                .FirstOrDefault (r => r.WorkbookAppId == Id)
                ?.TicketType;

            if (ticketType != null && !typeof (IAgentTicket).IsAssignableFrom (ticketType))
                throw new Exception (
                    $"Workbook app with ID {Id} registered invalid ticket type, ticket type must be " +
                    $"assignable to IAgentTicket"
                );
        }

        public async Task<IAgentTicket> RequestAgentTicketAsync (
            ClientSessionUri clientSessionUri,
            IMessageService messageService,
            Action disconnectedHandler,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            if (processManager == null)
                return null;

            if (ticketType != null)
                return (IAgentTicket)Activator.CreateInstance (
                    ticketType,
                    processManager,
                    messageService,
                    disconnectedHandler);

            var ticket = new AgentProcessTicket (
                processManager,
                messageService,
                disconnectedHandler);

            await ticket.GetAgentProcessStateAsync (cancellationToken);

            return ticket;
        }

        static IReadOnlyList<WorkbookAppInstallation> LocateWorkbookApps ()
        {
            FilePath repoRoot = DevEnvironment.RepositoryRootDirectory;
            if (!repoRoot.IsNull && repoRoot.DirectoryExists)
                searchPaths.Add (repoRoot.Combine ("_build"));

            var manifestFile = InteractiveInstallation
                .LocateFiles (searchPaths, "workbookapps.json")
                .FirstOrDefault ();

            if (manifestFile == null) {
                Log.Warning (TAG, "Unable to locate workbook apps manifest file:");
                foreach (var path in searchPaths)
                    Log.Warning (TAG, $"    {path}");
                return Array.Empty<WorkbookAppInstallation> ();
            }

            Log.Info (TAG, $"Loading workbook apps from manifest: {manifestFile}");

            var manifestDirectory = Path.GetDirectoryName (manifestFile);

            try {
                using (var reader = new StreamReader (manifestFile))
                    return JObject
                        .Load (new JsonTextReader (reader))
                        .Children<JProperty> ()
                        .Select (app => {
                            try {
                                return FromManifestObject (
                                    manifestDirectory,
                                    app.Name,
                                    (JObject)app.Value);
                            } catch (Exception e) {
                                Log.Error (TAG, e);
                                return null;
                            }
                        })
                        .Where (app => app != null)
                        .OrderBy (app => app.order)
                        .ToArray ();
            } catch (Exception e) {
                Log.Error (TAG, $"Unable to parse JSON for {manifestFile}", e);
            }

            return Array.Empty<WorkbookAppInstallation> ();
        }

        [EditorBrowsable (EditorBrowsableState.Never)]
        internal static WorkbookAppInstallation FromManifestObject (
            string manifestDirectory,
            string id,
            JObject appJson)
        {
            IEnumerable<string> Pathify (string path)
            {
                if (path == null)
                    return Array.Empty<string> ();

                var fullPath = Path.Combine (manifestDirectory, path);

                foreach (var mapper in pathMappers) {
                    fullPath = mapper (fullPath);
                    if (fullPath == null)
                        return Array.Empty<string> ();
                }

                if (path == "{systemGac}")
                    return GacCache.GacPaths;

                return new [] { fullPath };
            }

            var flavor = appJson.GetValue ("flavor")?.Value<string> ();
            if (flavor == null)
                return null;

            var sdkJson = appJson.GetValue ("sdk") as JObject;
            if (sdkJson == null)
                return null;

            var appPath = Pathify (appJson.GetValue ("appPath")?.Value<string> ()).FirstOrDefault ();
            if (appPath == null)
                return null;
            if (!new FilePath (appPath).Exists) {
                Log.Error (TAG, $"appPath invalid for {id}: {appPath}");
                return null;
            }

            var targetFramework = sdkJson.GetValue ("targetFramework")?.Value<string> ();
            if (targetFramework == null)
                return null;

            var assemblySearchPaths = sdkJson
                .GetValue ("assemblySearchPaths")
                ?.ToObject<string []> ()
                ?.SelectMany (Pathify)
                .ToArray ();
            if (assemblySearchPaths == null || assemblySearchPaths.Length == 0)
                return null;

            var order = appJson.GetValue ("order")?.Value<int> () ?? int.MaxValue;

            var sdk = new Sdk (
                id,
                new FrameworkName (targetFramework),
                assemblySearchPaths,
                sdkJson.GetValue ("name")?.Value<string> (),
                sdkJson.GetValue ("profile")?.Value<string> (),
                sdkJson.GetValue ("version")?.Value<string> ());

            return new WorkbookAppInstallation (
                id,
                flavor,
                appJson.GetValue ("icon")?.Value<string> (),
                appJson.GetValue ("optionalFeatures")?.ToObject<string []> (),
                sdk,
                appPath,
                Pathify (appJson.GetValue ("appManagerAssembly")?.Value<string> ())?.SingleOrDefault (),
                Pathify (appJson.GetValue ("ticketProviderAssembly")?.Value<string> ())?.SingleOrDefault (),
                order);
        }
    }
}