using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Controls;

namespace Xamarin.Interactive.Client.Windows
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.None)]
	[ComDefaultInterface (typeof (SHDocVw.DWebBrowserEvents2))]
	class NativeWebBrowserEventHandler : SHDocVw.DWebBrowserEvents2
	{
		public class BeforeNavigateCancelEventArgs : CancelEventArgs
		{
			public string Url { get; set; }
		}

		readonly int connectionCookie;
		readonly IConnectionPoint connectionPoint;

		static readonly PropertyInfo ActiveXInstanceProperty = typeof (WebBrowser).GetProperty (
			"ActiveXInstance",
			BindingFlags.Instance | BindingFlags.NonPublic);

		public event EventHandler<BeforeNavigateCancelEventArgs> BeforeNavigate;

		public NativeWebBrowserEventHandler (WebBrowser webBrowser)
		{
			var connectionPointContainer = (IConnectionPointContainer)
				ActiveXInstanceProperty.GetValue (webBrowser);
			var riid = typeof (SHDocVw.DWebBrowserEvents2).GUID;
			connectionPointContainer.FindConnectionPoint (ref riid, out connectionPoint);
			connectionPoint.Advise (this, out connectionCookie);
		}

		#region DWebBrowserEvents2 implementation

		public void BeforeNavigate2 (
			object pDisp,
			ref object URL,
			ref object Flags,
			ref object TargetFrameName,
			ref object PostData,
			ref object Headers,
			ref bool Cancel)
		{
			var args = new BeforeNavigateCancelEventArgs {
				Url = URL.ToString (),
			};
			BeforeNavigate?.Invoke (this, args);
			Cancel = args.Cancel;
		}

		#region Unused methods

		public void StatusTextChange (string Text)
		{
		}

		public void ProgressChange (int Progress, int ProgressMax)
		{
		}

		public void CommandStateChange (int Command, bool Enable)
		{
		}

		public void DownloadBegin ()
		{
		}

		public void DownloadComplete ()
		{
		}

		public void TitleChange (string Text)
		{
		}

		public void PropertyChange (string szProperty)
		{
		}

		public void NewWindow2 (ref object ppDisp, ref bool Cancel)
		{
		}

		public void NavigateComplete2 (object pDisp, ref object URL)
		{
		}

		public void DocumentComplete (object pDisp, ref object URL)
		{
		}

		public void OnQuit ()
		{
		}

		public void OnVisible (bool Visible)
		{
		}

		public void OnToolBar (bool ToolBar)
		{
		}

		public void OnMenuBar (bool MenuBar)
		{
		}

		public void OnStatusBar (bool StatusBar)
		{
		}

		public void OnFullScreen (bool FullScreen)
		{
		}

		public void OnTheaterMode (bool TheaterMode)
		{
		}

		public void WindowSetResizable (bool Resizable)
		{
		}

		public void WindowSetLeft (int Left)
		{
		}

		public void WindowSetTop (int Top)
		{
		}

		public void WindowSetWidth (int Width)
		{
		}

		public void WindowSetHeight (int Height)
		{
		}

		public void WindowClosing (bool IsChildWindow, ref bool Cancel)
		{
		}

		public void ClientToHostWindow (ref int CX, ref int CY)
		{
		}

		public void SetSecureLockIcon (int SecureLockIcon)
		{
		}

		public void FileDownload (bool ActiveDocument, ref bool Cancel)
		{
		}

		public void NavigateError (object pDisp, ref object URL, ref object Frame, ref object StatusCode, ref bool Cancel)
		{
		}

		public void PrintTemplateInstantiation (object pDisp)
		{
		}

		public void PrintTemplateTeardown (object pDisp)
		{
		}

		public void UpdatePageStatus (object pDisp, ref object nPage, ref object fDone)
		{
		}

		public void PrivacyImpactedStateChange (bool bImpacted)
		{
		}

		public void NewWindow3 (ref object ppDisp, ref bool Cancel, uint dwFlags, string bstrUrlContext, string bstrUrl)
		{
		}

		public void SetPhishingFilterStatus (int PhishingFilterStatus)
		{
		}

		public void WindowStateChanged (uint dwWindowStateFlags, uint dwValidFlagsMask)
		{
		}

		public void NewProcess (int lCauseFlag, object pWB2, ref bool Cancel)
		{
		}

		public void ThirdPartyUrlBlocked (ref object URL, uint dwCount)
		{
		}

		public void RedirectXDomainBlocked (
			object pDisp,
			ref object StartURL,
			ref object RedirectURL,
			ref object Frame,
			ref object StatusCode)
		{
		}

		public void BeforeScriptExecute (object pDispWindow)
		{
		}

		public void WebWorkerStarted (uint dwUniqueID, string bstrWorkerLabel)
		{
		}

		public void WebWorkerFinsihed (uint dwUniqueID)
		{
		}

		#endregion

		#endregion
	}
}
