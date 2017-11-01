//
// XcbWebBrowserSite.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Interop;

using System.Windows.Controls;
using System.Security;
using System.Runtime.InteropServices;

namespace Xamarin.CrossBrowser.Wpf.Internal
{
    class XcbWebBrowserSite : XcbActiveXSite,
        Com.IDocHostUIHandler,
        Com.IOleControlSite,
        Com.IServiceProvider,
        Com.IInternetSecurityManager
    {
        static readonly PropertyInfo WebBrowser_AxIWebBrowser2_Property = typeof (WebBrowser)
            .GetProperty ("AxIWebBrowser2", BindingFlags.NonPublic | BindingFlags.Instance);

        static readonly Type wbIDocHostUIHandlerType = LoadWindowsBaseComType ("IDocHostUIHandler");

        readonly WebBrowser webBrowser;

        public bool IsContextMenuEnabled { get; set; }

        public static XcbWebBrowserSite Bind (WebBrowser webBrowser)
        {
            if (webBrowser == null)
                throw new ArgumentNullException (nameof (webBrowser));

            var axBrowser = (SHDocVw.IWebBrowser2)WebBrowser_AxIWebBrowser2_Property.GetValue (webBrowser);
            var axBrowserOleObject = (Com.IOleObject)axBrowser;
            var webBrowserSite = new XcbWebBrowserSite (webBrowser, axBrowserOleObject.GetClientSite ());
            axBrowserOleObject.SetClientSite (webBrowserSite);
            return webBrowserSite;
        }

        XcbWebBrowserSite (WebBrowser webBrowser, object host) : base (host)
        {
            this.webBrowser = webBrowser;
        }

        #region IDocHostUIHandler

        int Com.IDocHostUIHandler.GetHostInfo (Native.DOCHOSTUIINFO info)
        {
            info.dwDoubleClick = (int)Native.DOCHOSTUIDBLCLICK.DEFAULT;
            info.dwFlags = (int)(
                // default flags from WPF WebBrowser
                Native.DOCHOSTUIFLAG.DISABLE_SCRIPT_INACTIVE |
                Native.DOCHOSTUIFLAG.ENABLE_INPLACE_NAVIGATION |
                Native.DOCHOSTUIFLAG.IME_ENABLE_RECONVERSION |
                Native.DOCHOSTUIFLAG.THEME |
                Native.DOCHOSTUIFLAG.ENABLE_FORMS_AUTOCOMPLETE |
                Native.DOCHOSTUIFLAG.DISABLE_UNTRUSTEDPROTOCOL |
                Native.DOCHOSTUIFLAG.LOCAL_MACHINE_ACCESS_CHECK |
                Native.DOCHOSTUIFLAG.ENABLE_REDIRECT_NOTIFICATION |
                Native.DOCHOSTUIFLAG.NO3DOUTERBORDER |
                // XCB flags
                Native.DOCHOSTUIFLAG.DPI_AWARE
            );

            return Native.S_OK;
        }

        int Com.IDocHostUIHandler.ShowContextMenu (
            int dwID,
            ref Native.POINT pt,
            object pcmdtReserved,
            object pdispReserved)
            => IsContextMenuEnabled ? Native.S_FALSE : Native.S_OK;

        int Com.IDocHostUIHandler.TranslateAccelerator (ref MSG msg, ref Guid group, int nCmdID)
            => Native.S_FALSE;

        int Com.IDocHostUIHandler.GetExternal (out object ppDispatch)
        {
            ppDispatch = webBrowser.ObjectForScripting;
            return Native.S_OK;
        }

        int Com.IDocHostUIHandler.GetOptionKeyPath (out string pbstrKey, int dw)
        {
            pbstrKey = null;
            return Native.E_NOTIMPL;
        }

        int Com.IDocHostUIHandler.ShowUI (
            int dwID,
            Com.IOleInPlaceActiveObject activeObject,
            Com.IOleCommandTarget commandTarget,
            Com.IOleInPlaceFrame frame,
            Com.IOleInPlaceUIWindow doc)
            => Native.E_NOTIMPL;

        int Com.IDocHostUIHandler.HideUI ()
            => Native.E_NOTIMPL;

        int Com.IDocHostUIHandler.UpdateUI ()
            => Native.E_NOTIMPL;

        int Com.IDocHostUIHandler.OnDocWindowActivate (bool fActivate)
            => Native.E_NOTIMPL;

        int Com.IDocHostUIHandler.OnFrameWindowActivate (bool fActivate)
            => Native.E_NOTIMPL;

        int Com.IDocHostUIHandler.ResizeBorder (
            Native.COMRECT rect,
            Com.IOleInPlaceUIWindow doc,
            bool fFrameWindow)
            => Native.E_NOTIMPL;

        int Com.IDocHostUIHandler.EnableModeless (bool fEnable)
            => Native.E_NOTIMPL;

        int Com.IDocHostUIHandler.FilterDataObject (IDataObject pDO, out IDataObject ppDORet)
        {
            ppDORet = null;
            return Native.E_NOTIMPL;
        }

        int Com.IDocHostUIHandler.GetDropTarget (Com.IOleDropTarget pDropTarget, out Com.IOleDropTarget ppDropTarget)
        {
            ppDropTarget = null;
            return Native.E_NOTIMPL;
        }

        int Com.IDocHostUIHandler.TranslateUrl (int dwTranslate, string strURLIn, out string pstrURLOut)
        {
            pstrURLOut = null;
            return Native.E_NOTIMPL;
        }

        #endregion

        #region IOleControlSite

        static MethodInfo iOleControlSite_TranslateAccelerator;

        [SecurityCritical]
        [SecuritySafeCritical]
        int Com.IOleControlSite.TranslateAccelerator (ref MSG msg, int grfModifiers)
        {
            var parameters = new object [] { msg, grfModifiers };
            var result = (int)Invoke (
                WB_IOleControlSiteType,
                ref iOleControlSite_TranslateAccelerator,
                parameters);
            msg = (MSG)parameters [0];
            return result;
        }

        #endregion

        #region IServiceProvider

        static readonly Guid IID_IInternetSecurityManager = Marshal.GenerateGuidForType (
            typeof (Com.IInternetSecurityManager));

        int Com.IServiceProvider.QueryService (
            ref Guid guidService,
            ref Guid riid,
            out IntPtr ppvObject)
        {
            if (guidService == IID_IInternetSecurityManager &&
                riid == IID_IInternetSecurityManager) {
                ppvObject = Marshal.GetComInterfaceForObject (
                    this,
                    typeof (Com.IInternetSecurityManager));
                return Native.S_OK;
            }

            ppvObject = IntPtr.Zero;
            return Native.E_NOINTERFACE;
        }

        #endregion

        #region IInternetSecurityManager

        unsafe int Com.IInternetSecurityManager.MapUrlToZone (
            string pwszUrl,
            int* pdwZone,
            int dwFlags)
        {
            *pdwZone = 0;
            return Native.S_OK;
        }

        unsafe int Com.IInternetSecurityManager.ProcessUrlAction (
            string pwszUrl,
            int dwAction,
            byte* pPolicy,
            int cbPolicy,
            byte* pContext,
            int cbContext,
            int dwFlags,
            int dwReserved)
        {
            *((int*)pPolicy) = 0; // ALLOW
            return Native.S_OK;
        }

        unsafe int Com.IInternetSecurityManager.SetSecuritySite (void* pSite)
            => Native.INET_E_DEFAULT_ACTION;

        unsafe int Com.IInternetSecurityManager.GetSecuritySite (void** ppSite)
            => Native.INET_E_DEFAULT_ACTION;

        unsafe int Com.IInternetSecurityManager.GetSecurityId (
            string pwszUrl,
            byte* pbSecurityId,
            int* pcbSecurityId,
            int dwReserved)
            => Native.INET_E_DEFAULT_ACTION;

        unsafe int Com.IInternetSecurityManager.QueryCustomPolicy (
            string pwszUrl,
            void* guidKey,
            byte** ppPolicy,
            int* pcbPolicy,
            byte* pContext,
            int cbContext,
            int dwReserved)
            => Native.INET_E_DEFAULT_ACTION;

        unsafe int Com.IInternetSecurityManager.SetZoneMapping (
            int dwZone,
            string lpszPattern,
            int dwFlags)
            => Native.INET_E_DEFAULT_ACTION;

        unsafe int Com.IInternetSecurityManager.GetZoneMappings (
            int dwZone,
            void** ppenumString,
            int dwFlags)
            => Native.INET_E_DEFAULT_ACTION;

        #endregion
    }
}