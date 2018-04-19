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
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Core
{
    abstract class Agent : IDisposable
    {
        string TAG => GetType ().Name;

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

        public ViewHierarchyHandlerManager ViewHierarchyHandlerManager { get; }
            = new ViewHierarchyHandlerManager ();

        public AgentIdentity Identity { get; protected set; }
        public ClientSessionUri ClientSessionUri { get; private set; }

        public MessageChannel MessageChannel { get; } = new MessageChannel ();

        readonly Lazy<EvaluationContextManager> evaluationContextManager;
        public EvaluationContextManager EvaluationContextManager => evaluationContextManager.Value;

        internal RepresentationManager RepresentationManager { get; } = new RepresentationManager ();

        readonly AgentServer agentServer;

        protected Agent () : this (false)
        {
        }

        protected Agent (bool unitTestContext)
        {
            MainThread.Initialize ();

            agentServer = new AgentServer (this);

            evaluationContextManager = new Lazy<EvaluationContextManager> (() => {
                MainThread.Ensure ();
                var host = CreateEvaluationContextManager ();
                host.Events.Subscribe (new Observer<ICodeCellEvent> (evnt => {
                    switch (evnt) {
                    case EvaluationInFlight _:
                        break;
                    default:
                        MessageChannel.Push (evnt);
                        break;
                    }
                }));
                return host;
            });

            if (!unitTestContext)
                RepresentationManager.AddProvider (new ReflectionRepresentationProvider ());
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                EvaluationContextManager.Dispose ();
                Stop ();
            }
        }

        protected virtual EvaluationContextManager CreateEvaluationContextManager ()
            => new EvaluationContextManager (RepresentationManager, this);

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

            InteractiveJsonSerializerSettings
                .SharedInstance
                .CreateSerializer ()
                .Serialize (
                    await request.GetRequestStreamAsync (),
                    Identity);

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