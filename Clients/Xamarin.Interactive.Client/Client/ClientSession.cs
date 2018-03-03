//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client.ViewControllers;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Compilation;
using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Reflection;
using Xamarin.Interactive.Workbook.LoadAndSave;
using Xamarin.Interactive.Workbook.Models;
using Xamarin.Interactive.Workbook.NewWorkbookFeatures;
using Xamarin.Interactive.Workbook.Views;

using static Xamarin.Interactive.Compilation.InteractiveDependencyResolver;

namespace Xamarin.Interactive.Client
{
    sealed class ClientSession : ISimplyObservable<ClientSessionEvent>, IDisposable
    {
        const string TAG = nameof (ClientSession);

        sealed class QuietlyDisposeClientSessionException : Exception
        {
        }

        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();
        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        readonly Uri clientWebServerUri;

        readonly Observable<ClientSessionEvent> observable = new Observable<ClientSessionEvent> ();

        // There are certain assemblies that we want to ban from being referenced because the explicit
        // references are not needed, and they make for Workbooks that aren't cross-platform in the
        // case of Xamarin.Forms.
        readonly IEnumerable<string> BannedReferencePrefixes = new [] {
            "Xamarin.Forms.Platform.",
            "FormsViewGroup",
        };

        bool isDisposed;

        public IClientSessionViewControllers ViewControllers { get; private set; }

        public ClientSessionUri Uri { get; }
        public Guid Id { get; }
        public ClientSessionKind SessionKind { get; }

        AgentConnection agent;
        public IAgentConnection Agent => agent;

        public IEvaluationService EvaluationService { get; private set; }
        public WorkbookPackage Workbook { get; }
        public WorkbookAppInstallation WorkbookApp { get; private set; }
        public RoslynCompilationWorkspace CompilationWorkspace { get; private set; }
        public FilePath WorkingDirectory { get; private set; }

        public bool CanEvaluate
            => CompilationWorkspace != null &&
               EvaluationService != null &&
               EvaluationService.CanEvaluate;

        sealed class ViewControllersProxy : IClientSessionViewControllers
        {
            public MessageViewController Messages { get; }
            public History ReplHistory { get; }
            public WorkbookTargetsViewController WorkbookTargets { get; }

            public ViewControllersProxy (
                ClientSession session,
                IClientSessionViewControllers viewControllers)
            {
                if (session.SessionKind == ClientSessionKind.LiveInspection)
                    ReplHistory = viewControllers.ReplHistory;

                Messages = viewControllers.Messages;
                WorkbookTargets = viewControllers.WorkbookTargets;
            }
        }

        public ClientSession (ClientSessionUri clientSessionUri)
        {
            if (clientSessionUri == null)
                throw new ArgumentNullException (nameof (clientSessionUri));

            Uri = clientSessionUri;
            SessionKind = clientSessionUri.SessionKind;
            agent = new AgentConnection (clientSessionUri.AgentType);

            Id = Guid.NewGuid ();

            clientWebServerUri = ClientApp.SharedInstance.WebServer.AddSession (this);

            Workbook = new WorkbookPackage (Uri.WorkbookPath);
            Workbook.PropertyChanged += Workbook_PropertyChanged;

            UpdateTitle ();
        }

        public void Dispose ()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (agent.IsConnected)
                agent.Api.AssociateClientSession (ClientSessionAssociationKind.Dissociating)
                     .ContinueWith (task => Workbook.Close ());
            else
                Workbook.Close ();

            ResetAgentConnection ();
            cancellationTokenSource.Cancel ();
            observable.Observers.OnCompleted ();
            EvaluationService?.Dispose ();
        }

        public void InitializeViewControllers (IClientSessionViewControllers viewControllers)
        {
            if (viewControllers == null)
                throw new ArgumentNullException (nameof (viewControllers));

            Action<object, string> assert = (viewController, name) => {
                if (viewController == null)
                    throw new ArgumentException (
                        $"{nameof (viewControllers)}.{nameof (name)} must not be null",
                        nameof (viewControllers));
            };

            assert (viewControllers.Messages, nameof (viewControllers.Messages));
            assert (viewControllers.ReplHistory, nameof (viewControllers.ReplHistory));
            assert (viewControllers.WorkbookTargets, nameof (viewControllers.WorkbookTargets));

            if (ViewControllers != null)
                ViewControllers.WorkbookTargets.PropertyChanged -= WorkbookTargets_PropertyChanged;

            ViewControllers = new ViewControllersProxy (this, viewControllers);
            ViewControllers.WorkbookTargets.UpdateTargets (this);
            ViewControllers.WorkbookTargets.PropertyChanged += WorkbookTargets_PropertyChanged;
        }

        void WorkbookTargets_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            EvaluationService.OutdateAllCodeCells ();

            var selectedTarget = ViewControllers.WorkbookTargets.SelectedTarget;
            if (selectedTarget == null)
                TerminateAgentConnection ();
            else if (selectedTarget.AgentType != agent.Type) {
                agent = agent.WithAgentType (selectedTarget.AgentType);
                InitializeAgentConnectionAsync ().Forget ();
            }
        }

        void Workbook_PropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof (WorkbookPackage.Title))
                UpdateTitle ();
        }

        #region Observable

        void PostEvent (ClientSessionEventKind eventKind)
        {
            var evnt = new ClientSessionEvent (this, eventKind);
            MainThread.Post (() => observable.Observers.OnNext (evnt));
        }

        void PostEvent (IObserver<ClientSessionEvent> observer, ClientSessionEventKind eventKind)
            => MainThread.Post (() => observer.OnNext (new ClientSessionEvent (this, eventKind)));

        public IDisposable Subscribe (IObserver<ClientSessionEvent> observer)
        {
            var subscription = observable.Subscribe (observer);

            PostEvent (observer, ClientSessionEventKind.SessionAvailable);
            PostEvent (observer, ClientSessionEventKind.SessionTitleUpdated);

            if (Agent.IsConnected)
                PostEvent (observer, ClientSessionEventKind.AgentConnected);
            else
                PostEvent (observer, ClientSessionEventKind.AgentDisconnected);

            PostEvent (observer, ClientSessionEventKind.AgentFeaturesUpdated);

            if (CompilationWorkspace != null)
                PostEvent (observer, ClientSessionEventKind.CompilationWorkspaceAvailable);

            return subscription;
        }

        public IDisposable Subscribe (Action<ClientSessionEvent> observer)
            => observable.Subscribe (new Observer<ClientSessionEvent> (observer));

        #endregion

        void ResetAgentConnection ()
        {
            ViewControllers.Messages.ClearStatusMessages ();

            var agentType = AgentType.Unknown;

            if (agent != null) {
                agentType = agent.Type;
                ((IDisposable)agent).Dispose ();
            }

            agent = new AgentConnection (agentType);
        }

        public void TerminateAgentConnection ()
            => agent = agent.TerminateConnection ();

        void AssertWorkbookSession ()
        {
            if (SessionKind != ClientSessionKind.Workbook)
                throw new InvalidOperationException ("not a workbook session");
        }

        public async Task InitializeAsync (IWorkbookPageHost workbookPageViewHost = null)
        {
            var genericLoadingMessage = SessionKind == ClientSessionKind.Workbook
                ? Catalog.GetString ("Loading workbook…")
                : Catalog.GetString ("Loading session…");

            var initializers = new List<ClientSessionTask> {
                ClientSessionTask.CreateRequired (genericLoadingMessage, LoadWorkbookAsync)
            };

            if (workbookPageViewHost != null) {
                initializers.AddRange (
                    workbookPageViewHost
                        .GetClientSessionInitializationTasks (clientWebServerUri)
                        .Select (t => ClientSessionTask.CreateRequired (genericLoadingMessage, t)));

                Task LoadWorkbookPageViewAsync (CancellationToken cancellationToken)
                {
                    var pageViewModel = workbookPageViewHost.CreatePageViewModel (this, Workbook.IndexPage);
                    EvaluationService = pageViewModel;

                    pageViewModel.LoadWorkbookPage ();

                    if (pageViewModel is IObserver<ClientSessionEvent> observer)
                        Subscribe (observer);

                    return Task.CompletedTask;
                }

                initializers.Add (ClientSessionTask.CreateRequired (genericLoadingMessage, LoadWorkbookPageViewAsync));
            }

            var initializeException = await RunInitializers (initializers);

            if (isDisposed)
                return;

            if (initializeException == null) {
                await InitializeAgentConnectionAsync ();

                try {
                    await ConfigureNewWorkbookFeatures ();
                } catch (Exception e) {
                    var message = Catalog.GetString (
                        "Unable to configure optional workbook features.");
                    Log.Warning (TAG, message, e);
                    ViewControllers.Messages.PushMessage (Message.CreateErrorStatus (
                        message,
                        e.Message));
                }

                return;
            }

            var genericErrorMessage = SessionKind == ClientSessionKind.Workbook
                ? Catalog.GetString ("Unable to load workbook.")
                : Catalog.GetString ("Unable to initialize live inspection session.");

            ViewControllers.Messages.PushMessage (Message.CreateErrorStatus (genericErrorMessage));

            ViewControllers.Messages.PushMessage (initializeException
                .ToAlertMessage (genericErrorMessage)
                .WithAction (new MessageAction (
                    MessageActionKind.Affirmative,
                    "close",
                    Catalog.GetString ("Close")))
                .WithActionResponseHandler ((message, action) => {
                    message.Dispose ();
                    Dispose ();
                }));
        }

        public async Task EnsureAgentConnectionAsync ()
        {
            if (!Agent.IsConnected)
                await InitializeAgentConnectionAsync ();
        }

        async Task InitializeAgentConnectionAsync ()
        {
            if (EvaluationService != null) {
                using (EvaluationService.InhibitEvaluate ())
                    await DoInitalizeAgentConnectionAsync ();
            } else {
                await DoInitalizeAgentConnectionAsync ();
            }
        }

        // Only call from InitializeAgentConnectionAsync
        async Task DoInitalizeAgentConnectionAsync ()
        {
            try {
                ResetAgentConnection ();

                using (ViewControllers.Messages.PushMessage (
                    Message.CreateInfoStatus (
                        Catalog.GetString ("Connecting to agent…"), showSpinner: true)))
                    await ConnectToAgentAsync (CancellationToken);

                using (ViewControllers.Messages.PushMessage (
                    Message.CreateInfoStatus (
                        Catalog.GetString ("Preparing workspace…"), showSpinner: true))) {
                    await InitializeCompilationWorkspaceAsync (CancellationToken);
                    if (EvaluationService == null)
                        EvaluationService = new EvaluationService (
                            CompilationWorkspace,
                            new EvaluationEnvironment (WorkingDirectory),
                            agent);
                }
            } catch (Exception e) {
                Log.Error (TAG, e);
                ViewControllers.Messages.PushMessage (WithReconnectSessionAction (e
                    .ToAlertMessage (Catalog.GetString ("Unable to connect"))));

                return;
            }

            try {
                using (ViewControllers.Messages.PushMessage (
                    Message.CreateInfoStatus (
                        Catalog.GetString ("Restoring packages…"), showSpinner: true)))
                    await InitializePackagesAsync (CancellationToken);
            } catch (Exception e) {
                Log.Error (TAG, e);
                ViewControllers.Messages.PushMessage (e
                    .ToAlertMessage (Catalog.GetString ("Unable to restore packages")));
            }
        }

        async Task<Exception> RunInitializers (IEnumerable<ClientSessionTask> initializers)
        {
            foreach (var initializer in initializers) {
                var message = Message.CreateInfoStatus (
                    initializer.Description,
                    showSpinner: true);

                message = ViewControllers.Messages.PushMessage (message);

                try {
                    await initializer.Delegate (CancellationToken);
                } catch (QuietlyDisposeClientSessionException) {
                    Dispose ();
                    return null;
                } catch (Exception e) {
                    Log.Error (TAG, $"InitializeAsync({initializer.Description})", e);

                    if (initializer.IsSuccessfulCompletionRequired)
                        return e;

                    if (initializer.ExceptionHandler != null) {
                        initializer.ExceptionHandler (e);
                        continue;
                    }

                    ViewControllers.Messages.PushMessage (Message.CreateErrorAlert (e.Message));
                } finally {
                    message.Dispose ();
                }
            }

            return null;
        }

        async Task LoadWorkbookAsync (CancellationToken cancellationToken)
        {
            bool cancelOpen = false;

            await Workbook.Open (async quarantineInfo => {
                var response = await ViewControllers.Messages.PushAlertMessageAsync (
                    quarantineInfo.CreateAlert ());
                cancelOpen = response.Id == "cancel";
                return !cancelOpen;
            }, Uri.WorkbookPath, agent.Type);

            if (cancelOpen)
                throw new QuietlyDisposeClientSessionException ();

            ViewControllers.WorkbookTargets.PropertyChanged -= WorkbookTargets_PropertyChanged;
            ViewControllers.WorkbookTargets.UpdateTargets (this);
            ViewControllers.WorkbookTargets.PropertyChanged += WorkbookTargets_PropertyChanged;

            if (ViewControllers.WorkbookTargets.SelectedTarget != null)
                agent = agent.WithAgentType (ViewControllers.WorkbookTargets.SelectedTarget.AgentType);
        }

        async Task ConfigureNewWorkbookFeatures ()
        {
            foreach (var parameter in Uri.Parameters) {
                switch (parameter.Key) {
                case "feature":
                    if (NewWorkbookFeature.AllFeatures.TryGetValue (
                        parameter.Value, out var feature))
                        await feature.ConfigureClientSession (
                            this,
                            CancellationToken);
                    break;
                }
            }
        }

        async Task ConnectToAgentAsync (CancellationToken cancellationToken)
        {
            if (agent.IsConnected)
                agent = agent.TerminateConnection ();

            if (SessionKind == ClientSessionKind.Workbook)
                WorkbookApp = WorkbookAppInstallation.Locate (agent.Type);

            agent = await agent.ConnectAsync (
                WorkbookApp,
                Uri,
                ViewControllers.Messages,
                HandleAgentDisconnected,
                cancellationTokenSource.Token);

            agent.Api.Messages.Subscribe (new Observer<object> (HandleAgentMessage));

            await agent.Api.SetLogLevelAsync (Log.GetLogLevel ());

            UpdateTitle ();

            PostEvent (ClientSessionEventKind.AgentConnected);

            new Telemetry.Models.AgentSession {
                AppSessionId = ClientApp.SharedInstance.AppSessionId,
                Timestamp = DateTimeOffset.UtcNow,
                Flavor = agent.Identity.FlavorId,
                Kind = (Telemetry.Models.AgentSessionKind)(int)SessionKind
            }.Post ();
        }

        void HandleAgentMessage (object message)
        {
            if (message is Evaluation result && result.InitializedAgentIntegration)
                RefreshForAgentIntegration ().Forget ();
        }

        void HandleAgentDisconnected ()
        {
            var disconnectedAgent = agent;

            ResetAgentConnection ();
            EvaluationService.OutdateAllCodeCells ();

            PostEvent (ClientSessionEventKind.AgentDisconnected);

            var title = Catalog.GetString ("Session Disconnected");

            Message message;
            if (SessionKind == ClientSessionKind.Workbook)
                message = WithReconnectSessionAction (
                    Message.CreateErrorAlert (title, Catalog.GetString (
                        "The Workbook host application has terminated.")));
            else
                message = Message.CreateInfoAlert (
                    title,
                    Catalog.Format (Catalog.GetString (
                        "Inspector is no longer connected to {0}. You may review and close " +
                        "the session window at your leisure. A new Inspector session may be " +
                        "attached via the debugger in your IDE."),
                        disconnectedAgent.Identity.ApplicationName));

            ViewControllers.Messages.PushMessage (message);
        }

        Message WithReconnectSessionAction (Message message) => message
            .WithAction (
                new MessageAction (
                    MessageActionKind.Affirmative,
                    MessageAction.RetryActionId,
                    Catalog.GetString ("Reconnect"),
                    Catalog.GetString ("Reconnect session")))
            .WithActionResponseHandler (
                async (m, a) => await InitializeAgentConnectionAsync ());

        async Task InitializeCompilationWorkspaceAsync (CancellationToken cancellationToken)
        {
            WorkingDirectory = Workbook.WorkingBasePath;
            if (!WorkingDirectory.DirectoryExists)
                WorkingDirectory = Uri.WorkingDirectory;
            if (!WorkingDirectory.DirectoryExists)
                WorkingDirectory = FilePath.Empty;

            if (agent.IsConnected) {
                await GacCache.InitializingTask;
                await Agent.Api.AssociateClientSession (
                    ClientSessionAssociationKind.Initial,
                    WorkingDirectory);
                CompilationWorkspace = await CompilationWorkspaceFactory.CreateWorkspaceAsync (this);
            }

            await RefreshForAgentIntegration ();

            if (CompilationWorkspace == null)
                throw new Exception ("Unable to get compilation workspace for agent.");

            var dependencyResolver = CompilationWorkspace.DependencyResolver;

            if (WorkingDirectory.DirectoryExists) {
                dependencyResolver.BaseDirectory = WorkingDirectory;
                dependencyResolver.AddAssemblySearchPath (WorkingDirectory);
            }

            Workbook.WorkingPathChanged += (o, e) => {
                if (dependencyResolver != null) {
                    dependencyResolver.RemoveAssemblySearchPath (WorkingDirectory);
                    dependencyResolver.RemoveAssemblySearchPath (e.OldBasePath);

                    WorkingDirectory = e.NewBasePath;
                    dependencyResolver.BaseDirectory = WorkingDirectory;
                    dependencyResolver.AddAssemblySearchPath (WorkingDirectory);
                }
            };

            PostEvent (ClientSessionEventKind.CompilationWorkspaceAvailable);
        }

        public string Title { get; private set; }
        public string SecondaryTitle { get; private set; }

        void UpdateTitle ()
        {
            SecondaryTitle = null;

            if (SessionKind == ClientSessionKind.Workbook) {
                Title = Workbook.Title;
                if (WorkbookApp != null)
                    SecondaryTitle = WorkbookAppViewController.GetDisplayLabel (
                        WorkbookApp,
                        WorkbookAppViewController.Context.StatusBar);
            } else {
                Title = agent.Identity?.ApplicationName ?? Catalog.GetString ("Live Inspect Session");

                var sdk = agent.Identity?.Sdk;
                SecondaryTitle = sdk?.Name;
                if (SecondaryTitle != null && sdk.Profile != null)
                    SecondaryTitle = Catalog.Format (Catalog.GetString (
                        "{0} ({1} Profile)",
                        comment: "{0} is a runtime/product name like 'Xamarin.Mac'; " +
                            "{1} is a profile name like 'Modern'"),
                        sdk.Name,
                        sdk.Profile);
            }

            PostEvent (ClientSessionEventKind.SessionTitleUpdated);
        }

        public IWorkbookSaveOperation CreateWorkbookSaveOperation ()
        {
            AssertWorkbookSession ();
            return Workbook.CreateSaveOperation ();
        }

        public void SaveWorkbook (IWorkbookSaveOperation saveOperation)
        {
            AssertWorkbookSession ();
            Workbook.Save (saveOperation);
            UpdateTitle ();

            // Update working directory in agent. If not connected, this will happen when creating the
            // compilation workspace.
            if (Agent.IsConnected)
                Agent.Api.AssociateClientSession (
                    ClientSessionAssociationKind.Reassociating,
                    Workbook.WorkingBasePath).Forget ();
        }

        async Task RefreshForAgentIntegration ()
        {
            agent = await agent.RefreshFeaturesAsync ();
            PostEvent (ClientSessionEventKind.AgentFeaturesUpdated);
        }

        #region NuGet Package Management

        public bool CanAddPackages => SessionKind == ClientSessionKind.Workbook && Workbook.Packages != null;

        async Task InitializePackagesAsync (CancellationToken cancellationToken)
        {
            if (SessionKind != ClientSessionKind.Workbook)
                return;

            var alreadyInstalledPackages = Workbook.Packages == null
                ? ImmutableArray<InteractivePackage>.Empty
                : Workbook.Packages.InstalledPackages;

            Workbook.Packages = new InteractivePackageManager (
                WorkbookApp.Sdk.TargetFramework,
                ClientApp
                    .SharedInstance
                    .Paths
                    .CacheDirectory
                    .Combine ("package-manager"));

            var packages = Workbook
                .Pages
                .SelectMany (page => page.Packages)
                .Concat (alreadyInstalledPackages)
                .Where (p => p.IsExplicit)
                .Distinct (PackageIdComparer.Default)
                .ToArray ();

            if (packages.Length == 0)
                return;

            await RestorePackagesAsync (packages, cancellationToken);

            foreach (var package in Workbook.Packages.InstalledPackages)
                await LoadPackageIntegrationsAsync (package, cancellationToken);
        }

        async Task RestorePackagesAsync (
            IEnumerable<InteractivePackage> packages,
            CancellationToken cancellationToken)
        {
            await Workbook.Packages.RestorePackagesAsync (packages, cancellationToken);

            foreach (var package in Workbook.Packages.InstalledPackages) {
                ReferencePackageInWorkspace (package);
                await ReferenceTopLevelPackageAsync (package, cancellationToken);
            }
        }

        public async Task InstallPackageAsync (
            PackageViewModel packageViewModel,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            var package = new InteractivePackage (packageViewModel.Package);

            var installedPackages = await Workbook.Packages.InstallPackageAsync (
                package,
                packageViewModel.SourceRepository,
                cancellationToken);
            // TODO: Should probably alert user that the package is already installed.
            //       Should we add a fresh #r for the package in case that's what they're trying to get?
            //       A feel good thing?
            if (installedPackages.Count == 0)
                return;

            foreach (var installedPackage in installedPackages) {
                ReferencePackageInWorkspace (installedPackage);
                await LoadPackageIntegrationsAsync (installedPackage, cancellationToken);
            }

            // TODO: Figure out metapackages. Install Microsoft.AspNet.SignalR, for example,
            //       and no #r submission gets generated, so all the workspace reference stuff
            //       above fails to bring in references to dependnet assemblies automatically.
            //       User must type them out themselves.
            //
            //       This was busted in our NuGet 2.x code as well.
            package = installedPackages.FirstOrDefault (
                p => PackageIdComparer.Equals (p, package));

            // TODO: Same issue as installedPackages.Count == 0. What do we want to tell user?
            //       probably they tried to install a package they already had installed, and
            //       maybe it bumped a shared dep (which is why installedPackages is non-empty).
            if (package == null)
                return;

            if (await ReferenceTopLevelPackageAsync (package, cancellationToken)) {
                EvaluationService.OutdateAllCodeCells ();
                await EvaluationService.EvaluateAllAsync (cancellationToken);
            }
        }

        async Task LoadPackageIntegrationsAsync (
            InteractivePackage package,
            CancellationToken cancellationToken)
        {
            // Forms is special-cased because we own it and load the extension from our framework.
            if (PackageIdComparer.Equals (package.Identity.Id, "Xamarin.Forms")) {
                await CompilationWorkspaceFactory.LoadFormsAgentExtensions (
                    package.Identity.Version.Version,
                    this,
                    CompilationWorkspace.DependencyResolver,
                    CompilationWorkspace.EvaluationContextId,
                    Agent.IncludePeImage);
            }

            var assembliesToLoadOnAgent = new List<ResolvedAssembly> ();

            // Integration assemblies are not expected to be in a TFM directory—we look for them in
            // the `xamarin.interactive` folder inside the NuGet package.
            var packagePath = Workbook
                .Packages
                .GetPackageInstallPath (package);

            var interactivePath = packagePath.Combine ("xamarin.interactive");

            if (interactivePath.DirectoryExists) {
                var interactiveAssemblies = interactivePath.EnumerateFiles ("*.dll");
                foreach (var interactiveReference in interactiveAssemblies) {
                    var resolvedAssembly = CompilationWorkspace
                        .DependencyResolver
                        .ResolveWithoutReferences (interactiveReference);

                    if (HasIntegration (resolvedAssembly)) {
                        assembliesToLoadOnAgent.Add (resolvedAssembly);

                        foreach (var dependency in resolvedAssembly.ExternalDependencies) {
                            if (!(dependency is WebDependency))
                                continue;

                            if (AddNuGetWebResource (dependency.Location, out var id))
                                await EvaluationService.LoadWorkbookDependencyAsync ($"/static/{id}");
                        }
                    }
                }
            }

            if (assembliesToLoadOnAgent.Count > 0) {
                var assembliesToLoad = assembliesToLoadOnAgent.Select (dep => {
                    var peImage = Agent.IncludePeImage
                        ? GetFileBytes (dep.Path)
                        : null;
                    var syms = Agent.IncludePeImage
                        ? GetDebugSymbolsFromAssemblyPath (dep.Path)
                        : null;
                    return new AssemblyDefinition (
                        dep.AssemblyName,
                        dep.Path,
                        peImage: peImage,
                        debugSymbols: syms
                    );
                }).ToArray ();

                await Agent.Api.LoadAssembliesAsync (
                    CompilationWorkspace.EvaluationContextId,
                    assembliesToLoad);
            }

            await RefreshForAgentIntegration ();
        }

        void ReferencePackageInWorkspace (InteractivePackage package)
        {
            foreach (var packageAssemblyReference in package.AssemblyReferences)
                CompilationWorkspace.DependencyResolver.AddAssemblySearchPath (
                    packageAssemblyReference.ParentDirectory);
        }

        async Task<bool> ReferenceTopLevelPackageAsync (
            InteractivePackage package,
            CancellationToken cancellationToken)
        {
            if (package.AssemblyReferences.Count == 0)
                return false;

            var references = new List<string> ();

            foreach (var packageAssemblyReference in package.AssemblyReferences) {
                var resolvedAssembly = CompilationWorkspace
                    .DependencyResolver
                    .ResolveWithoutReferences (packageAssemblyReference);
                if (resolvedAssembly == null)
                    continue;

                if (BannedReferencePrefixes.Any (resolvedAssembly.AssemblyName.Name.StartsWith))
                    continue;

                // Don't add #r for integration assemblies.
                if (HasIntegration (resolvedAssembly))
                    continue;

                references.Add (resolvedAssembly.AssemblyName.Name);
            }

            return await EvaluationService.AddTopLevelReferencesAsync (references, cancellationToken);
        }

        bool HasIntegration (ResolvedAssembly resolvedAssembly)
        {
            try {
                var refAsm = Assembly.LoadFrom (resolvedAssembly.Path);

                if (refAsm == null)
                    return false;

                if (refAsm.GetReferencedAssemblies ().Any (r => r.Name == "Xamarin.Interactive")) {
                    var integrationType = refAsm
                        .GetCustomAttribute<AgentIntegrationAttribute> ()
                        ?.AgentIntegrationType;

                    if (integrationType != null)
                        return true;
                }

                return false;
            } catch (Exception e) {
                Log.Warning (TAG,
                    $"Couldn't load assembly {resolvedAssembly.AssemblyName.Name} for " +
                    $"agent integration loading",
                    e);
                return false;
            }
        }

        #endregion

        #region Web Resources

        ImmutableBidirectionalDictionary<Guid, FilePath> webResources
            = ImmutableBidirectionalDictionary<Guid, FilePath>.Empty;

        public bool AddNuGetWebResource (FilePath path, out string id)
        {
            ClientApp
                .SharedInstance
                .WebServer
                .AddStaticResource (id = Guid.NewGuid () + path.Extension, path);

            Log.Debug (TAG, $"NuGet path {path} as {id}");

            return true;
        }

        public bool AddWebResource (FilePath path, out Guid guid)
        {
            if (webResources.TryGetFirst (path, out guid))
                return false;

            webResources = webResources.Add (guid = Guid.NewGuid (), path);

            Log.Debug (TAG, $"{path} as {guid}");

            return true;
        }

        public bool TryGetWebResource (Guid guid, out FilePath path)
            => webResources.TryGetSecond (guid, out path);

        #endregion
    }
}