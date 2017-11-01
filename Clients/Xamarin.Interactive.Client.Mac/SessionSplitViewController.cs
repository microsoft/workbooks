//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    // NOTE: keep in sync with SessionViewController
    abstract class SessionSplitViewController : NSSplitViewController, IObserver<ClientSessionEvent>
    {
        readonly SessionViewControllerAdapter<SessionSplitViewController> adapter;

        protected SessionSplitViewController (IntPtr handle) : base (handle)
        {
            adapter = new SessionViewControllerAdapter<SessionSplitViewController> (this);
        }

        public ClientSession Session => adapter.Session;

        public override void ViewDidAppear ()
        {
            base.ViewDidAppear ();
            adapter.ViewDidAppear ();
        }

        #region IObserver<ClientSessionEvent>

        void IObserver<ClientSessionEvent>.OnNext (ClientSessionEvent evnt)
        {
            switch (evnt.Kind) {
            case ClientSessionEventKind.SessionAvailable:
                OnSessionAvailable ();
                break;
            case ClientSessionEventKind.SessionTitleUpdated:
                OnSessionTitleUpdated ();
                break;
            case ClientSessionEventKind.AgentConnected:
                OnAgentConnected ();
                break;
            case ClientSessionEventKind.AgentFeaturesUpdated:
                OnAgentFeaturesUpdated ();
                break;
            case ClientSessionEventKind.AgentDisconnected:
                OnAgentDisconnected ();
                break;
            case ClientSessionEventKind.CompilationWorkspaceAvailable:
                OnCompilationWorkspaceAvailable ();
                break;
            default:
                throw new NotImplementedException (evnt.Kind.ToString ());
            }
        }

        void IObserver<ClientSessionEvent>.OnError (Exception error)
        {
        }

        void IObserver<ClientSessionEvent>.OnCompleted ()
        {
        }

        protected virtual void OnSessionAvailable ()
        {
        }

        protected virtual void OnSessionTitleUpdated ()
        {
        }

        protected virtual void OnAgentConnected ()
        {
        }

        protected virtual void OnAgentFeaturesUpdated ()
        {
        }

        protected virtual void OnAgentDisconnected ()
        {
        }

        protected virtual void OnCompilationWorkspaceAvailable ()
        {
        }

        #endregion
    }
}