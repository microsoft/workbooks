//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;

namespace Xamarin.CrossBrowser.Wpf.Internal
{
    static class Com
    {
        [ComImport]
        [Guid ("00000100-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumUnknown
        {
            [PreserveSig]
            int Next (
                [In, MarshalAs (UnmanagedType.U4)] int celt,
                [Out] IntPtr rgelt,
                IntPtr pceltFetched);

            [PreserveSig]
            int Skip ([In, MarshalAs (UnmanagedType.U4)] int celt);

            void Reset ();

            void Clone ([Out] out IEnumUnknown ppenum);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("00000104-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumOLEVERB
        {
            [PreserveSig]
            int Next (
                [MarshalAs (UnmanagedType.U4)] int celt,
                [Out] Native.tagOLEVERB rgelt,
                [Out, MarshalAs (UnmanagedType.LPArray)]
                int[] pceltFetched);

            [PreserveSig]
            int Skip ([In, MarshalAs (UnmanagedType.U4)] int celt);

            void Reset ();

            void Clone (out IEnumOLEVERB ppenum);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("00000112-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleObject
        {
            [PreserveSig]
            int SetClientSite (
                [In, MarshalAs (UnmanagedType.Interface)] IOleClientSite pClientSite);

            // IOleClientSite GetClientSite ();
            [return: MarshalAs (UnmanagedType.Interface)]
            object GetClientSite ();

            [PreserveSig]
            int SetHostNames (
                [In, MarshalAs (UnmanagedType.LPWStr)] string szContainerApp,
                [In, MarshalAs (UnmanagedType.LPWStr)] string szContainerObj);

            [PreserveSig]
            int Close (int dwSaveOption);

            [PreserveSig]
            int SetMoniker (
                [In, MarshalAs (UnmanagedType.U4)] int dwWhichMoniker,
                [In, MarshalAs (UnmanagedType.Interface)] object pmk);

            [PreserveSig]
            int GetMoniker (
                [In, MarshalAs (UnmanagedType.U4)] int dwAssign,
                [In, MarshalAs (UnmanagedType.U4)] int dwWhichMoniker,
                [Out, MarshalAs (UnmanagedType.Interface)] out object moniker);

            [PreserveSig]
            int InitFromData (
                [In, MarshalAs (UnmanagedType.Interface)] IDataObject pDataObject,
                int fCreation,
                [In, MarshalAs (UnmanagedType.U4)] int dwReserved);

            [PreserveSig]
            int GetClipboardData (
                [In, MarshalAs (UnmanagedType.U4)] int dwReserved,
                out IDataObject data);

            [PreserveSig]
            int DoVerb (
                int iVerb,
                [In] IntPtr lpmsg,
                [In, MarshalAs (UnmanagedType.Interface)] IOleClientSite pActiveSite,
                int lindex,
                IntPtr hwndParent,
                [In] Native.COMRECT lprcPosRect);

            [PreserveSig]
            int EnumVerbs (out IEnumOLEVERB e);

            [PreserveSig]
            int OleUpdate ();

            [PreserveSig]
            int IsUpToDate ();

            [PreserveSig]
                int GetUserClassID (
                [In, Out] ref Guid pClsid);

            [PreserveSig]
            int GetUserType (
                [In, MarshalAs (UnmanagedType.U4)] int dwFormOfType,
                [Out, MarshalAs (UnmanagedType.LPWStr)] out string userType);

            [PreserveSig]
            int SetExtent (
                [In, MarshalAs (UnmanagedType.U4)] int dwDrawAspect,
                [In] Native.SIZE pSizel);

            [PreserveSig]
            int GetExtent (
                [In, MarshalAs (UnmanagedType.U4)] int dwDrawAspect,
                [Out] Native.SIZE pSizel);

            [PreserveSig]
            int Advise (IAdviseSink pAdvSink, out int cookie);

            [PreserveSig]
            int Unadvise (
                [In, MarshalAs (UnmanagedType.U4)] int dwConnection);

            [PreserveSig]
            int EnumAdvise (out IEnumSTATDATA e);

            [PreserveSig]
            int GetMiscStatus (
                [In, MarshalAs (UnmanagedType.U4)] int dwAspect,
                out int misc);

            [PreserveSig]
            int SetColorScheme (
                [In] Native.tagLOGPALETTE pLogpal);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("0000011B-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleContainer
        {
            [PreserveSig]
            int ParseDisplayName (
                [In, MarshalAs (UnmanagedType.Interface)] object pbc,
                [In, MarshalAs (UnmanagedType.BStr)] string pszDisplayName,
                [Out, MarshalAs (UnmanagedType.LPArray)] int[] pchEaten,
                [Out, MarshalAs (UnmanagedType.LPArray)] object[] ppmkOut);

            [PreserveSig]
            int EnumObjects (
                [In, MarshalAs (UnmanagedType.U4)] int grfFlags,
                [Out] out IEnumUnknown ppenum);

            [PreserveSig]
            int LockContainer (bool fLock);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("B196B289-BAB4-101A-B69C-00AA00341D07")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleControlSite
        {
            [PreserveSig]
            int OnControlInfoChanged ();

            [PreserveSig]
            int LockInPlaceActive (int fLock);

            [PreserveSig]
            int GetExtendedControl (
                [Out, MarshalAs (UnmanagedType.IDispatch)] out object ppDisp);

            [PreserveSig]
            int TransformCoords (
                [In, Out] Native.POINT pPtlHimetric,
                [In, Out] Native.POINTF pPtfContainer,
                [In, MarshalAs (UnmanagedType.U4)] int dwFlags);

            [PreserveSig]
            int TranslateAccelerator (
                [In] ref System.Windows.Interop.MSG pMsg,
                [In, MarshalAs (UnmanagedType.U4)] int grfModifiers);

            [PreserveSig]
            int OnFocus (int fGotFocus);

            [PreserveSig]
            int ShowPropertyFrame ();
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("00000118-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleClientSite
        {
            [PreserveSig]
            int SaveObject ();

            [PreserveSig]
            int GetMoniker (
                [In, MarshalAs (UnmanagedType.U4)] int dwAssign,
                [In, MarshalAs (UnmanagedType.U4)] int dwWhichMoniker,
                [Out, MarshalAs (UnmanagedType.Interface)] out object moniker);

            [PreserveSig]
            int GetContainer (
                [Out, MarshalAs (UnmanagedType.Interface)] out /*IOleContainer*/ object container);

            [PreserveSig]
            int ShowObject ();

            [PreserveSig]
            int OnShowWindow (int fShow);

            [PreserveSig]
            int RequestNewObjectLayout ();
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("00000115-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceUIWindow
        {
            IntPtr GetWindow ();

            [PreserveSig]
            int ContextSensitiveHelp (int fEnterMode);

            [PreserveSig]
            int GetBorder ([Out] Native.RECT lprectBorder);

            [PreserveSig]
            int RequestBorderSpace ([In] Native.RECT pborderwidths);

            [PreserveSig]
            int SetBorderSpace ([In] Native.RECT pborderwidths);

            void SetActiveObject (
                   [In, MarshalAs (UnmanagedType.Interface)] IOleInPlaceActiveObject pActiveObject,
                   [In, MarshalAs (UnmanagedType.LPWStr)] string pszObjName);
        }

        [ComImport]
        [Guid ("00000117-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceActiveObject
        {
            [PreserveSig]
            int GetWindow (out IntPtr hwnd);

            void ContextSensitiveHelp (int fEnterMode);

            [PreserveSig]
            int TranslateAccelerator ([In] ref System.Windows.Interop.MSG lpmsg);

            void OnFrameWindowActivate (int fActivate);

            void OnDocWindowActivate (int fActivate);

            void ResizeBorder (
                [In] Native.RECT prcBorder,
                [In] IOleInPlaceUIWindow pUIWindow,
                bool fFrameWindow);

            void EnableModeless (int fEnable);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("00000116-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceFrame
        {
            IntPtr GetWindow ();

            [PreserveSig]
            int ContextSensitiveHelp (int fEnterMode);

            [PreserveSig]
            int GetBorder ([Out] Native.COMRECT lprectBorder);

            [PreserveSig]
            int RequestBorderSpace ([In] Native.COMRECT pborderwidths);

            [PreserveSig]
            int SetBorderSpace ([In] Native.COMRECT pborderwidths);

            [PreserveSig]
            int SetActiveObject (
                [In, MarshalAs (UnmanagedType.Interface)] IOleInPlaceActiveObject pActiveObject,
                [In, MarshalAs (UnmanagedType.LPWStr)] string pszObjName);

            [PreserveSig]
            int InsertMenus (
                [In] IntPtr hmenuShared,
                [In, Out] Native.tagOleMenuGroupWidths lpMenuWidths);

            [PreserveSig]
            int SetMenu (
                [In] IntPtr hmenuShared,
                [In] IntPtr holemenu,
                [In] IntPtr hwndActiveObject);

            [PreserveSig]
            int RemoveMenus (
                [In] IntPtr hmenuShared);

            [PreserveSig]
            int SetStatusText ([In, MarshalAs (UnmanagedType.LPWStr)] string pszStatusText);

            [PreserveSig]
            int EnableModeless (bool fEnable);

            [PreserveSig]
            int TranslateAccelerator (
                [In] ref System.Windows.Interop.MSG lpmsg,
                [In, MarshalAs (UnmanagedType.U2)] short wID);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("00000119-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceSite
        {
            IntPtr GetWindow ();

            [PreserveSig]
            int ContextSensitiveHelp (int fEnterMode);

            [PreserveSig]
            int CanInPlaceActivate ();

            [PreserveSig]
            int OnInPlaceActivate ();

            [PreserveSig]
            int OnUIActivate ();

            [PreserveSig]
            int GetWindowContext (
                [Out, MarshalAs (UnmanagedType.Interface)] out IOleInPlaceFrame ppFrame,
                [Out, MarshalAs (UnmanagedType.Interface)] out IOleInPlaceUIWindow ppDoc,
                [Out] Native.COMRECT lprcPosRect,
                [Out] Native.COMRECT lprcClipRect,
                [In, Out] Native.OLEINPLACEFRAMEINFO lpFrameInfo);

            [PreserveSig]
            int Scroll (Native.SIZE scrollExtant);

            [PreserveSig]
            int OnUIDeactivate (int fUndoable);

            [PreserveSig]
            int OnInPlaceDeactivate ();

            [PreserveSig]
            int DiscardUndoState ();

            [PreserveSig]
            int DeactivateAndUndo ();

            [PreserveSig]
            int OnPosRectChange ([In] Native.COMRECT lprcPosRect);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("9BFBBC02-EFF1-101A-84ED-00AA00341D07")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPropertyNotifySink
        {
            void OnChanged (int dispID);

            [PreserveSig]
            int OnRequestEdit (int dispID);
        }

        // Helper GUID type for nullability requirement in IOleCommandTarget.Exec.
        [StructLayout (LayoutKind.Sequential)]
        public class GUID
        {
            public Guid guid;

            public GUID (Guid guid)
            {
                this.guid = guid;
            }
        }

        [StructLayout (LayoutKind.Sequential)]
        public class OLECMD
        {
            [MarshalAs (UnmanagedType.U4)]
            public int cmdID = 0;
            [MarshalAs (UnmanagedType.U4)]
            public int cmdf = 0;
        }

        [SuppressUnmanagedCodeSecurity]
        [ComVisible (true)]
        [ComImport]
        [Guid ("B722BCCB-4E68-101B-A2BC-00AA00404770")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleCommandTarget
        {
            [SecurityCritical]
            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int QueryStatus (
                GUID pguidCmdGroup, /* nullable GUID */
                int cCmds,
                [In, Out] OLECMD prgCmds,
                [In, Out] IntPtr pCmdText);

            [SecurityCritical]
            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int Exec (
                GUID pguidCmdGroup, /* nullable GUID */
                int nCmdID,
                int nCmdexecopt,
                // we need to have this an array because callers need to be able to specify NULL or VT_NULL
                [In, MarshalAs (UnmanagedType.LPArray)] object[] pvaIn,
                int pvaOut);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("00000122-0000-0000-C000-000000000046")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleDropTarget
        {
            [PreserveSig]
            int OleDragEnter (
                [In, MarshalAs (UnmanagedType.Interface)] object pDataObj,
                [In, MarshalAs (UnmanagedType.U4)] int grfKeyState,
                [In, MarshalAs (UnmanagedType.U8)] long pt,
                [In, Out] ref int pdwEffect);

            [PreserveSig]
            int OleDragOver (
                [In, MarshalAs (UnmanagedType.U4)] int grfKeyState,
                [In, MarshalAs (UnmanagedType.U8)] long pt,
                [In, Out] ref int pdwEffect);

            [PreserveSig]
            int OleDragLeave ();

            [PreserveSig]
            int OleDrop (
                [In, MarshalAs (UnmanagedType.Interface)] object pDataObj,
                [In, MarshalAs (UnmanagedType.U4)] int grfKeyState,
                [In, MarshalAs (UnmanagedType.U8)] long pt,
                [In, Out] ref int pdwEffect);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("BD3F23C0-D43E-11CF-893B-00AA00BDCE1A")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDocHostUIHandler
        {
            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int ShowContextMenu (
                [In, MarshalAs (UnmanagedType.U4)] int dwID,
                [In] ref Native.POINT pt,
                [In, MarshalAs (UnmanagedType.Interface)] object pcmdtReserved,
                [In, MarshalAs (UnmanagedType.Interface)] object pdispReserved);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int GetHostInfo (
                [In, Out] Native.DOCHOSTUIINFO info);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int ShowUI (
                [In, MarshalAs (UnmanagedType.I4)] int dwID,
                [In] IOleInPlaceActiveObject activeObject,
                [In] IOleCommandTarget commandTarget,
                [In] IOleInPlaceFrame frame,
                [In] IOleInPlaceUIWindow doc);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int HideUI ();

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int UpdateUI ();

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int EnableModeless (
                [In, MarshalAs (UnmanagedType.Bool)] bool fEnable);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int OnDocWindowActivate (
                [In, MarshalAs (UnmanagedType.Bool)] bool fActivate);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int OnFrameWindowActivate (
                [In, MarshalAs (UnmanagedType.Bool)] bool fActivate);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int ResizeBorder (
                [In] Native.COMRECT rect,
                [In] IOleInPlaceUIWindow doc,
                bool fFrameWindow);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int TranslateAccelerator (
                [In] ref System.Windows.Interop.MSG msg,
                [In] ref Guid group,
                [In, MarshalAs (UnmanagedType.I4)] int nCmdID);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int GetOptionKeyPath (
                [Out, MarshalAs (UnmanagedType.LPWStr)] out string pbstrKey,
                [In, MarshalAs (UnmanagedType.U4)] int dw);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int GetDropTarget (
                [In, MarshalAs (UnmanagedType.Interface)] IOleDropTarget pDropTarget,
                [Out, MarshalAs (UnmanagedType.Interface)] out IOleDropTarget ppDropTarget);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int GetExternal (
                [Out, MarshalAs (UnmanagedType.IDispatch)] out object ppDispatch);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int TranslateUrl (
                [In, MarshalAs (UnmanagedType.U4)] int dwTranslate,
                [In, MarshalAs (UnmanagedType.LPWStr)] string strURLIn,
                [Out, MarshalAs (UnmanagedType.LPWStr)] out string pstrURLOut);

            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int FilterDataObject (
                IDataObject pDO,
                out IDataObject ppDORet);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [Guid ("6D5140C1-7436-11CE-8034-00AA006009FA")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IServiceProvider
        {
            [SuppressUnmanagedCodeSecurity]
            [SecurityCritical]
            [return: MarshalAs (UnmanagedType.I4)]
            [PreserveSig]
            int QueryService (
                ref Guid guidService,
                ref Guid riid,
                out IntPtr ppvObject);
        }

        [ComImport]
        [Guid ("79eac9ee-baf9-11ce-8c82-00aa004ba90b")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInternetSecurityMgrSite
        {
            void GetWindow (/* [out] */ ref IntPtr phwnd);
            void EnableModeless (/* [in] */ bool fEnable);
        }

        [SuppressUnmanagedCodeSecurity]
        [ComImport]
        [ComVisible (false)]
        [Guid ("79eac9ee-baf9-11ce-8c82-00aa004ba90b")]
        [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IInternetSecurityManager
        {
            [PreserveSig]
            unsafe int SetSecuritySite (void* pSite);

            [PreserveSig]
            unsafe int GetSecuritySite (void** ppSite);

            [PreserveSig]
            unsafe int MapUrlToZone (
                [In, MarshalAs (UnmanagedType.LPWStr)] string pwszUrl,
                int* pdwZone,
                [In] int dwFlags);

            [PreserveSig]
            unsafe int GetSecurityId (
                [In, MarshalAs (UnmanagedType.LPWStr)] string pwszUrl,
                byte* pbSecurityId,
                int* pcbSecurityId,
                int dwReserved);

            [PreserveSig]
            unsafe int ProcessUrlAction (
                [In, MarshalAs (UnmanagedType.LPWStr)] string pwszUrl,
                int dwAction,
                byte* pPolicy,
                int cbPolicy,
                byte* pContext,
                int cbContext,
                int dwFlags,
                int dwReserved);

            [PreserveSig]
            unsafe int QueryCustomPolicy (
                [In, MarshalAs (UnmanagedType.LPWStr)] string pwszUrl,
                void* guidKey,
                byte** ppPolicy,
                int* pcbPolicy,
                byte* pContext,
                int cbContext,
                int dwReserved);

            [PreserveSig]
            unsafe int SetZoneMapping (
                int dwZone,
                [In, MarshalAs (UnmanagedType.LPWStr)] string lpszPattern,
                int dwFlags);

            [PreserveSig]
            unsafe int GetZoneMappings (
                int dwZone,
                void** ppenumString,
                int dwFlags);
        }
    }
}