//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Core
{
    abstract class Agent : IAgent, IDisposable
    {
        string TAG => GetType ().Name;

        public static Assembly [] AppDomainStartupAssemblies { get; }
            = AppDomain.CurrentDomain.GetAssemblies ();

        static Agent ()
        {
            if (!Log.IsInitialized)
                Log.Initialize (new LogProvider (LogLevel.Info, null));

            AppDomain.CurrentDomain.AssemblyLoad += AppDomain_AssemblyLoad;

            InteractiveCulture.Initialize ();
        }

        static void AppDomain_AssemblyLoad (object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.GetName ().Name == "System.Net.Http")
                InitializeHttpClientHandler (args.LoadedAssembly);
        }

        public event EventHandler IdentificationFailure;

        public IAgentSynchronizationContext SynchronizationContexts { get; }
            = new AgentSynchronizationContext ();

        public RepresentationManager RepresentationManager { get; } = new RepresentationManager ();
        IRepresentationManager IAgent.RepresentationManager => RepresentationManager;

        public ViewHierarchyHandlerManager ViewHierarchyHandlerManager { get; }
            = new ViewHierarchyHandlerManager ();

        public AgentIdentity Identity { get; protected set; }
        public ClientSessionUri ClientSessionUri { get; private set; }

        public MessageChannel MessageChannel { get; } = new MessageChannel ();

        readonly AgentServer agentServer;

        readonly IList<Action> resetStateHandlers = new List<Action> ();

        readonly Dictionary<EvaluationContextId, EvaluationContext> evaluationContexts =
            new Dictionary<EvaluationContextId, EvaluationContext> ();

        public EvaluationContext CreateEvaluationContext ()
        {
            var globalObject = CreateEvaluationContextGlobalObject ();
            var context = new EvaluationContext (this, globalObject);
            globalObject.EvaluationContext = context;
            evaluationContexts [context.Id] = context;
            return context;
        }

        public EvaluationContext GetEvaluationContext (EvaluationContextId contextId)
        {
            if (evaluationContexts.TryGetValue (contextId, out var context))
                return context;

            throw new ArgumentException ($"No execution context found with session ID {contextId}");
        }

        protected Agent () : this (false)
        {
        }

        protected Agent (bool unitTestContext)
        {
            agentServer = new AgentServer (this);

            if (!unitTestContext)
                RepresentationManager.AddProvider (new ReflectionRepresentationProvider ());
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            Stop ();

            foreach (var context in evaluationContexts.Values)
                context?.Dispose ();
            evaluationContexts.Clear ();
        }

        protected virtual EvaluationContextGlobalObject CreateEvaluationContextGlobalObject ()
        {
            return new EvaluationContextGlobalObject (this);
        }

        /// <summary>
        /// Called by execution context for any native libraries sent along with a RemoteAssembly.
        /// </summary>
        public virtual void LoadExternalDependencies (
            Assembly loadedAssembly,
            AssemblyDependency [] externalDependencies)
        {
        }

        /// <summary>
        /// Reset UI state, for workbooks that are doing a fresh execution.
        /// </summary>
        public void ResetState ()
        {
            // Let the platform-specific agents do their work first.
            HandleResetState ();

            // Then allow the integrations to participate.
            foreach (var resettingStateHandler in resetStateHandlers) {
                try {
                    resettingStateHandler.Invoke ();
                } catch (Exception e) {
                    Log.Warning (TAG, "Registered reset state handler threw exception.", e);
                }
            }
        }

        protected virtual void HandleResetState ()
        {
        }

        public void RegisterResetStateHandler (Action handler)
        {
            if (handler == null)
                throw new ArgumentNullException (nameof (handler));

            resetStateHandlers.Add (handler);
        }

        public void PublishEvaluation (
            CodeCellId codeCellid,
            object result,
            EvaluationResultHandling resultHandling = EvaluationResultHandling.Replace)
            => MainThread.Post (() => PublishEvaluation (new Evaluation {
                CodeCellId = codeCellid,
                ResultHandling = resultHandling,
                Result = RepresentationManager.Prepare (result)
            }));

        internal void PublishEvaluation (Evaluation result)
            => MessageChannel.Push (result);

        internal virtual IEnumerable<string> GetReplDefaultUsingNamespaces ()
        {
            return new [] {
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Threading",
                "System.Threading.Tasks",
                "Xamarin.Interactive.CodeAnalysis.Workbooks"
            };
        }

        internal virtual IEnumerable<string> GetReplDefaultWarningSuppressions ()
            => Array.Empty<string> ();

        public abstract InspectView GetVisualTree (string hierarchyKind);

        public virtual InspectView HighlightView (double x, double y, bool clear, string hierarchyKind)
        {
            return null;
        }

        /// <summary>
        /// Sets the value of the member represented by <paramref name="memberInfo"/> on the object pointed to
        /// by <paramref name="handle"/>.
        /// </summary>
        /// <returns><c>true</c>, if member was set, <c>false</c> otherwise.</returns>
        /// <param name="handle">The cache handle to the object on which we want to set a value.</param>
        /// <param name="memberInfo">The member which we want to set. Can be a field or a proprety.</param>
        /// <param name="value">The new value.</param>
        /// <param name="returnUpdatedValue">
        /// If set to <c>true</c>, set <paramref name="updatedValue"/> to the updated value of the object.
        /// </param>
        /// <param name="updatedValue">The updated value of the object.</param>
        public bool TrySetObjectMember (
            long handle,
            RepresentedMemberInfo memberInfo,
            object value,
            bool returnUpdatedValue,
            out InteractiveObject updatedValue)
        {
            updatedValue = null;

            var translatedValue = value;
            var propertyType = memberInfo.MemberType.ResolvedType;
            if (value != null) {
                object convertedValue;
                if (RepresentationManager.TryConvertFromRepresentation (
                    memberInfo.MemberType,
                    new [] { value },
                    out convertedValue))
                    translatedValue = convertedValue;
            }

            // TODO: Special handling if null value comes in for non-nullable property?
            try {
                if (translatedValue != null) {
                    if (propertyType.IsEnum)
                        translatedValue = Enum.ToObject (propertyType, translatedValue);
                    else if (translatedValue.GetType () != propertyType)
                        translatedValue = Convert.ChangeType (translatedValue, propertyType);
                }
            } catch (InvalidCastException ice) {
                Log.Error (TAG, $"Cannot convert from {translatedValue.GetType ().Name} to {propertyType.Name}");
                throw ice;
            }

            var target = ObjectCache.Shared.GetObject (handle);
            memberInfo.SetValue (target, translatedValue);

            if (returnUpdatedValue)
                updatedValue = RepresentationManager.PrepareInteractiveObject (target);

            return true;
        }

        public Agent Start (AgentStartOptions startOptions = null)
        {
            agentServer.Start ();
            Identity = Identity
                .WithHost (agentServer.BaseUri.Host)
                .WithPort ((ushort)agentServer.BaseUri.Port);

            // Default to LiveInspection, and correct later if wrong.
            // Only inspection extensions respond to InspectorSupport.AgentStarted.
            ClientSessionUri = new ClientSessionUri (
                Identity.AgentType,
                startOptions?.ClientSessionKind ?? ClientSessionKind.LiveInspection,
                Identity.Host,
                Identity.Port);

            startOptions?.AgentStarted?.Invoke (ClientSessionUri);

            try {
                var identifyAgentRequest = GetIdentifyAgentRequest ();

                if (identifyAgentRequest != null) {
                    ClientSessionUri = ClientSessionUri.WithSessionKind (ClientSessionKind.Workbook);
                    WriteAgentIdentityAsync (identifyAgentRequest).ContinueWithOnMainThread (task => {
                        if (task.IsFaulted) {
                            Log.Error (
                                TAG,
                                $"{nameof (WriteAgentIdentityAsync)}",
                                task.Exception);
                            IdentificationFailure?.Invoke (this, EventArgs.Empty);
                        }
                    });
                }
            } catch (Exception e) {
                Log.Error (TAG, e);
                IdentificationFailure?.Invoke (this, EventArgs.Empty);
            }

            Log.Info (TAG, $"{Identity.AgentType} '{Identity.ApplicationName}' "
                + $"is available for interaction: {ClientSessionUri}");

            return this;
        }

        protected virtual IdentifyAgentRequest GetIdentifyAgentRequest ()
        {
            return null;
        }

        async Task WriteAgentIdentityAsync (IdentifyAgentRequest identifyAgentRequest)
        {
            if (identifyAgentRequest == null)
                throw new ArgumentNullException (nameof (identifyAgentRequest));

            var request = WebRequest.CreateHttp (identifyAgentRequest.Uri);
            request.Method = "POST";

            new XipSerializer (
                await request.GetRequestStreamAsync (),
                InteractiveSerializerSettings.SharedInstance).Serialize (Identity);

            var response = (HttpWebResponse)await request.GetResponseAsync ();

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception ($"Client returned HTTP status code {response.StatusCode}");
        }

        public void Stop ()
            => agentServer.Stop ();

        /// <summary>
        /// Set the logging level for the agent.
        /// </summary>
        /// <param name="newLogLevel">The new log level.</param>
        public virtual void SetLogLevel (LogLevel newLogLevel)
            => Log.SetLogLevel (newLogLevel);

        /// <summary>
        /// Change the current working directory. Pass FilePath.Empty for a temp path.
        /// </summary>
        public virtual void ChangeDirectory (FilePath path)
        {
            if (path == Environment.CurrentDirectory)
                return;
            if (!path.DirectoryExists)
                path = FilePath.GetTempPath ();
            Log.Info (TAG, $"{Environment.CurrentDirectory} → {path}");
            Environment.CurrentDirectory = path;
        }

        #region HttpClient

        static Type snhHttpClientHandlerType;

        static void InitializeHttpClientHandler (Assembly assembly)
            => snhHttpClientHandlerType = assembly.GetType ("System.Net.Http.HttpClientHandler");

        static object CreateHttpClientHandler ()
            => snhHttpClientHandlerType == null
                ? null
                : Activator.CreateInstance (snhHttpClientHandlerType);

        Func<object> createDefaultHttpMessageHandler;
        public Func<object> CreateDefaultHttpMessageHandler {
            get { return createDefaultHttpMessageHandler ?? CreateHttpClientHandler; }
            set { createDefaultHttpMessageHandler = value; }
        }

        #endregion
    }
}