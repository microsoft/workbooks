//
// XcbActiveXSite.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Interop;

namespace Xamarin.CrossBrowser.Wpf.Internal
{
	class XcbActiveXSite :
		Com.IOleControlSite,
		Com.IOleClientSite,
		Com.IOleInPlaceSite,
		Com.IPropertyNotifySink
	{
		const string WindowsBaseAssembly = "WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

		protected static Type LoadWindowsBaseComType (string comTypeName)
			=> Type.GetType ($"MS.Win32.UnsafeNativeMethods+{comTypeName}, {WindowsBaseAssembly}");

		protected static readonly Type WB_IOleControlSiteType = LoadWindowsBaseComType ("IOleControlSite");
		protected static readonly Type WB_IOleClientSiteType = LoadWindowsBaseComType ("IOleClientSite");
		protected static readonly Type WB_IOleInPlaceSiteType = LoadWindowsBaseComType ("IOleInPlaceSite");
		protected static readonly Type WB_IPropertyNotifySinkType = LoadWindowsBaseComType ("IPropertyNotifySink");

		readonly object host;

		public XcbActiveXSite (object host)
		{
			this.host = host;
		}

		protected object Invoke (
			Type type,
			ref MethodInfo methodInfo,
			object [] parameters,
			[CallerMemberName] string methodName = "")
		{
			if (methodInfo == null)
				methodInfo = type.GetMethod (methodName);
			return methodInfo.Invoke (host, parameters);;
		}

		protected object Invoke (
			Type type,
			ref MethodInfo methodInfo,
			[CallerMemberName] string methodName = "",
			object [] parameters = null)
			=> Invoke (type, ref methodInfo, parameters, methodName);

		#region IOleControlSite

		int Com.IOleControlSite.GetExtendedControl (out object ppDisp)
		{
			ppDisp = null;
			return Native.E_NOTIMPL;
		}

		int Com.IOleControlSite.LockInPlaceActive (int fLock)
			=> Native.E_NOTIMPL;

		int Com.IOleControlSite.OnControlInfoChanged ()
			=> Native.S_OK;

		int Com.IOleControlSite.OnFocus (int fGotFocus)
			=> Native.S_OK;

		int Com.IOleControlSite.ShowPropertyFrame ()
			=> Native.E_NOTIMPL;

		int Com.IOleControlSite.TransformCoords (Native.POINT pPtlHimetric, Native.POINTF pPtfContainer, int dwFlags)
		{
			throw new NotImplementedException ();
		}

		int Com.IOleControlSite.TranslateAccelerator (ref MSG pMsg, int grfModifiers)
			=> Native.S_FALSE;
		#endregion

		#region IOleClientSite

		static MethodInfo iOleClientSite_GetContainer;

		int Com.IOleClientSite.GetContainer (out object container)
		{
			var parameters = new object [1];
			var result = (int)Invoke (WB_IOleClientSiteType, ref iOleClientSite_GetContainer, parameters);
			container = parameters [0];
			return result;
		}

		int Com.IOleClientSite.GetMoniker (int dwAssign, int dwWhichMoniker, out object moniker)
		{
			moniker = null;
			return Native.E_NOTIMPL;
		}

		int Com.IOleClientSite.OnShowWindow (int fShow)
			=> Native.S_OK;

		int Com.IOleClientSite.RequestNewObjectLayout ()
			=> Native.E_NOTIMPL;

		int Com.IOleClientSite.SaveObject ()
			=> Native.E_NOTIMPL;

		static MethodInfo iOleClientSite_ShowObject;

		int Com.IOleClientSite.ShowObject ()
			=> (int)Invoke (WB_IOleClientSiteType, ref iOleClientSite_ShowObject);

		#endregion

		#region IOleInPlaceSite

		int Com.IOleInPlaceSite.CanInPlaceActivate ()
			=> Native.S_OK;

		int Com.IOleInPlaceSite.ContextSensitiveHelp (int fEnterMode)
			=> Native.E_NOTIMPL;

		int Com.IOleInPlaceSite.DeactivateAndUndo ()
		{
			throw new NotImplementedException ();
		}

		int Com.IOleInPlaceSite.DiscardUndoState ()
			=> Native.S_OK;

		static MethodInfo iOleInPlaceSite_GetWindow;

		IntPtr Com.IOleInPlaceSite.GetWindow ()
			=> (IntPtr)Invoke (WB_IOleInPlaceSiteType, ref iOleInPlaceSite_GetWindow);

		int Com.IOleInPlaceSite.GetWindowContext (
			out Com.IOleInPlaceFrame ppFrame,
			out Com.IOleInPlaceUIWindow ppDoc,
			Native.COMRECT lprcPosRect,
			Native.COMRECT lprcClipRect,
			Native.OLEINPLACEFRAMEINFO lpFrameInfo)
		{
			throw new NotImplementedException ();
		}

		int Com.IOleInPlaceSite.OnInPlaceActivate ()
		{
			throw new NotImplementedException ();
		}

		int Com.IOleInPlaceSite.OnInPlaceDeactivate ()
		{
			throw new NotImplementedException ();
		}

		int Com.IOleInPlaceSite.OnPosRectChange (Native.COMRECT lprcPosRect)
		{
			throw new NotImplementedException ();
		}

		int Com.IOleInPlaceSite.OnUIActivate ()
		{
			throw new NotImplementedException ();
		}

		int Com.IOleInPlaceSite.OnUIDeactivate (int fUndoable)
		{
			throw new NotImplementedException ();
		}

		int Com.IOleInPlaceSite.Scroll (Native.SIZE scrollExtant)
			=> Native.S_FALSE;

		#endregion

		#region IPropertyNotifySink

		void Com.IPropertyNotifySink.OnChanged (int dispID)
		{
			throw new NotImplementedException ();
		}

		int Com.IPropertyNotifySink.OnRequestEdit (int dispID)
			=> Native.S_OK;

		#endregion
	}
}