//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Rendering.Renderers;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering
{
    sealed class RendererContext
    {
        const string TAG = nameof (RendererContext);

        public ClientSession ClientSession { get; }
        public IReadOnlyDictionary<string, object> Options { get; }
        public HtmlDocument Document { get; }
        public JavaScriptRendererRegistry Renderers { get; }

        public event EventHandler<MemberReferenceRequestArgs> MemberReferenceRequested;
        public event EventHandler<AsyncRenderCompleteEventArgs> AsyncRenderComplete;

        public RendererContext (ClientSession clientSession, HtmlDocument document)
        {
            ClientSession = clientSession
                ?? throw new ArgumentNullException (nameof (clientSession));

            Options = ImmutableDictionary.Create<string, object> ().Add (
                // Only allow getting quick references to interactive object
                // members during user app inspection. In workbooks, the use
                // of cache handles would be problematic.
                InteractiveObjectRenderer.OPTION_SHOW_REF_MEMBER,
                clientSession.SessionKind == ClientSessionKind.LiveInspection
            );

            Document = document;

            Renderers = new JavaScriptRendererRegistry (Document);
            Renderers.Initialize ();
        }

        public void Render (RenderState renderState, HtmlElement targetElem)
            => new RootRenderer (this, renderState).Render (targetElem);

        public void NotifyAsyncRenderComplete (RenderState renderState)
            => AsyncRenderComplete?.Invoke (this, new AsyncRenderCompleteEventArgs (renderState));

        public async Task<IInteractiveObject> InteractAsync (
            IRenderer renderer,
            IInteractiveObject source,
            object message = null)
        {
            if (!ClientSession.Agent.IsConnected)
                return null;

            try {
                return await ClientSession.Agent.Api.InteractAsync (source, message);
            } catch (Exception e) {
                Log.Error (TAG, e);
                return null;
            }
        }

        public async Task<bool> SetMemberAsync (RemoteMemberInfo memberInfo, object value)
        {
            if (!ClientSession.Agent.IsConnected)
                return false;

            return (await ClientSession.Agent.Api.SetObjectMemberAsync (
                memberInfo.ObjectHandle,
                memberInfo.MemberInfo,
                value,
                false)).Success;
        }

        public void RaiseMemberReferenceRequested (MemberReferenceRequestArgs args)
        {
            if (args == null)
                throw new ArgumentNullException (nameof (args));
            MemberReferenceRequested?.Invoke (this, args);
        }
    }
}