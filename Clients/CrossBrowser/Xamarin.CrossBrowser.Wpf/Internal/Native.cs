//
// Native.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
//
// API definitions adapted from WindowsBase reference source.

using System;
using System.Runtime.InteropServices;

namespace Xamarin.CrossBrowser.Wpf.Internal
{
	static class Native
	{
		public const int
			S_OK = 0x00000000,
			S_FALSE = 0x00000001;

		public const int
			E_NOTIMPL = unchecked((int)0x80004001),
			E_NOINTERFACE = unchecked((int)0x80004002),
			INET_E_DEFAULT_ACTION = unchecked((int)0x800C0011);

		[StructLayout (LayoutKind.Sequential)]
		public struct SIZE
		{
			public int cx;
			public int cy;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct POINTF
		{
			public float x;
			public float y;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct COMRECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout (LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
		public sealed class tagOleMenuGroupWidths
		{
			[MarshalAs (UnmanagedType.ByValArray, SizeConst = 6)/*leftover(offset=0, widths)*/]
			public int [] widths = new int [6];
		}

		[StructLayout (LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
		public sealed class tagOLEVERB
		{
			public int lVerb = 0;

			[MarshalAs (UnmanagedType.LPWStr)] // leftover(offset=4, customMarshal="UniStringMarshaller", lpszVerbName)
			public string lpszVerbName = null;

			[MarshalAs (UnmanagedType.U4)] // leftover(offset=8, fuFlags)
			public uint fuFlags = 0;

			[MarshalAs (UnmanagedType.U4)] // leftover(offset=12, grfAttribs)
			public uint grfAttribs = 0;
		}

		[StructLayout (LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
		public sealed class tagLOGPALETTE
		{
			[MarshalAs (UnmanagedType.U2)] // leftover(offset=0, palVersion)
			public ushort palVersion = 0;

			[MarshalAs (UnmanagedType.U2)] // leftover(offset=2, palNumEntries)
			public ushort palNumEntries = 0;
		}

		[StructLayout (LayoutKind.Sequential)/*leftover(noAutoOffset)*/]
		public sealed class OLEINPLACEFRAMEINFO
		{
			[MarshalAs (UnmanagedType.U4)/*leftover(offset=0, cb)*/]
			public uint cb;

			public bool fMDIApp;
			public IntPtr hwndFrame;
			public IntPtr hAccel;

			[MarshalAs (UnmanagedType.U4)/*leftover(offset=16, cAccelEntries)*/]
			public uint cAccelEntries;
		}

		[ComVisible (true)]
		[StructLayout (LayoutKind.Sequential)]
		public class DOCHOSTUIINFO
		{
			[MarshalAs (UnmanagedType.U4)]
			int cbSize = Marshal.SizeOf (typeof (DOCHOSTUIINFO));

			[MarshalAs (UnmanagedType.I4)]
			public int dwFlags;

			[MarshalAs (UnmanagedType.I4)]
			public int dwDoubleClick;

			[MarshalAs (UnmanagedType.I4)]
			int dwReserved1 = 0;

			[MarshalAs (UnmanagedType.I4)]
			int dwReserved2 = 0;
		}

		[Flags]
		public enum DOCHOSTUIDBLCLICK
		{
			DEFAULT = 0x0,
			SHOWPROPERTIES = 0x1,
			SHOWCODE = 0x2
		}

		[Flags]
		public enum DOCHOSTUIFLAG
		{
			DIALOG = 0x00000001,
			DISABLE_HELP_MENU = 0x00000002,
			NO3DBORDER = 0x00000004,
			SCROLL_NO = 0x00000008,
			DISABLE_SCRIPT_INACTIVE = 0x00000010,
			OPENNEWWIN = 0x00000020,
			DISABLE_OFFSCREEN = 0x00000040,
			FLAT_SCROLLBAR = 0x00000080,
			DIV_BLOCKDEFAULT = 0x00000100,
			ACTIVATE_CLIENTHIT_ONLY = 0x00000200,
			OVERRIDEBEHAVIORFACTORY = 0x00000400,
			CODEPAGELINKEDFONTS = 0x00000800,
			URL_ENCODING_DISABLE_UTF8 = 0x00001000,
			URL_ENCODING_ENABLE_UTF8 = 0x00002000,
			ENABLE_FORMS_AUTOCOMPLETE = 0x00004000,
			ENABLE_INPLACE_NAVIGATION = 0x00010000,
			IME_ENABLE_RECONVERSION = 0x00020000,
			THEME = 0x00040000,
			NOTHEME = 0x00080000,
			NOPICS = 0x00100000,
			NO3DOUTERBORDER = 0x00200000,
			DISABLE_EDIT_NS_FIXUP = 0x00400000,
			LOCAL_MACHINE_ACCESS_CHECK = 0x00800000,
			DISABLE_UNTRUSTEDPROTOCOL = 0x01000000,
			HOST_NAVIGATES = 0x02000000,
			ENABLE_REDIRECT_NOTIFICATION = 0x04000000,
			USE_WINDOWLESS_SELECTCONTROL = 0x08000000,
			USE_WINDOWED_SELECTCONTROL = 0x10000000,
			ENABLE_ACTIVEX_INACTIVATE_MODE = 0x20000000,
			DPI_AWARE = 0x40000000
		}

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct OLECMDTEXT
		{
			public uint cmdtextf;
			public uint cwActual;
			public uint cwBuf;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 100)]
			public char rgwz;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct OLECMD
		{
			public uint cmdID;
			public uint cmdf;
		}
	}
}