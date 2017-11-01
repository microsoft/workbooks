//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;

namespace Xamarin.Interactive.Client
{
    sealed class ClientSessionController<TApplicationState> where TApplicationState : class
    {
        struct State
        {
            public ClientSession Session;
            public TApplicationState ApplicationState;
        }

        ImmutableDictionary<ClientSession, State> sessions = ImmutableDictionary<ClientSession, State>.Empty;

        public void AddSession (ClientSession clientSession, TApplicationState applicationState)
        {
            if (clientSession == null)
                throw new ArgumentNullException (nameof (clientSession));

            if (applicationState == null)
                throw new ArgumentNullException (nameof (applicationState));

            sessions = sessions.Add (clientSession, new State {
                Session = clientSession,
                ApplicationState = applicationState
            });
        }

        public void RemoveSession (ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException (nameof (clientSession));

            sessions = sessions.Remove (clientSession);
        }

        public bool TryGetApplicationState (
            ClientSession clientSession,
            out TApplicationState applicationState)
        {
            if (clientSession == null)
                throw new ArgumentNullException (nameof (clientSession));

            State state;
            if (sessions.TryGetValue (clientSession, out state)) {
                applicationState = state.ApplicationState;
                return true;
            }

            applicationState = null;
            return false;
        }

        public bool TryGetApplicationState (
            ClientSessionUri clientSessionUri,
            out TApplicationState applicationState)
        {
            if (clientSessionUri == null)
                throw new ArgumentNullException (nameof (clientSessionUri));

            foreach (var state in sessions.Values) {
                switch (state.Session.SessionKind) {
                case ClientSessionKind.Workbook:
                    if (clientSessionUri.WorkbookPath != null &&
                        state.Session.Workbook.LogicalPath == clientSessionUri.WorkbookPath) {
                        applicationState = state.ApplicationState;
                        return true;
                    }

                    break;
                case ClientSessionKind.LiveInspection:
                    if (state.Session.Uri == clientSessionUri) {
                        applicationState = state.ApplicationState;
                        return true;
                    }

                    break;
                }
            }

            applicationState = null;
            return false;
        }
    }
}
