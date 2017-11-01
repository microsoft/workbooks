// 
// Authorization.cs: 
//
// Authors: Miguel de Icaza
//     
// Copyright 2012 Xamarin Inc
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if MONOMAC

#if false
using XamCore.ObjCRuntime;
using XamCore.Foundation;
#else
using ObjCRuntime;
using Foundation;
#endif

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;

namespace XamCore.Security {
	// Untyped enum in ObjC
	public enum AuthorizationStatus {
		Success                 = 0,
		InvalidSet              = -60001,
		InvalidRef              = -60002,
		InvalidTag              = -60003,
		InvalidPointer          = -60004,
		Denied                  = -60005,
		Canceled                = -60006,
		InteractionNotAllowed   = -60007,
		Internal                = -60008,
		ExternalizeNotAllowed   = -60009,
		InternalizeNotAllowed   = -60010,
		InvalidFlags            = -60011,
		ToolExecuteFailure      = -60031,
		ToolEnvironmentError    = -60032,
		BadAddress              = -60033,
	}

	// typedef UInt32 AuthorizationFlags;
	[Flags]
	public enum AuthorizationFlags : int {
		Defaults,
		InteractionAllowed = 1 << 0,
		ExtendRights = 1 << 1,
		PartialRights = 1 << 2,
		DestroyRights = 1 << 3,
		PreAuthorize = 1 << 4,
		NoData = 1 << 20
	}

#if !XAMCORE_4_0
	//
	// For ease of use, we let the user pass the AuthorizationParameters, and we
	// create the structure for them with the proper data
	//
	[Obsolete ("Use AuthorizationRights")]
	public class AuthorizationParameters {
		[Obsolete ("Set the 'system.privilege.admin' key on AuthorizationRights")]
		[Deprecated (PlatformName.MacOSX, 10, 7)]
		public string PathToSystemPrivilegeTool;

		[Obsolete ("Set AuthorizationEnvironment.Prompt")]
		public string Prompt;

		[Obsolete ("Set AuthorizationEnvironment.IconPath")]
		public string IconPath;

		internal AuthorizationRights ToAuthorizationRights ()
		{
			// NOTE: this is #define, not a resolvable symbol (also deprecated in 10.7)
			const string kAuthorizationRightExecute = "system.privilege.admin";

			var rights = new AuthorizationRights ();

			if (PathToSystemPrivilegeTool != null)
				rights.Add (kAuthorizationRightExecute, PathToSystemPrivilegeTool);

			return rights;
		}
	}
#endif

	public class AuthorizationEnvironment {
#if XAMCORE_4_0
		public string Username { get; set; }
		public string Password { get; set; }
		public bool AddToSharedCredentialPool { get; set; }
#else
		public string Username;
		public string Password;
		public bool   AddToSharedCredentialPool;
#endif

		public string Prompt { get; set; }
		public string IconPath { get; set; }

		internal AuthorizationItemSet ToAuthorizationItemSet ()
		{
			// NOTE: these are all #define, not resolvable symbols
			const string kAuthorizationEnvironmentUsername = "username";
			const string kAuthorizationEnvironmentPassword = "password";
			const string kAuthorizationEnvironmentShared = "shared";
			const string kAuthorizationEnvironmentPrompt = "prompt";
			const string kAuthorizationEnvironmentIcon = "icon";

			var items = new AuthorizationItemSet ();

			if (Username != null)
				items.Add (kAuthorizationEnvironmentUsername, Username);

			if (Password != null)
				items.Add (kAuthorizationEnvironmentPassword, Password);

			if (AddToSharedCredentialPool)
				items.Add (kAuthorizationEnvironmentShared);

			if (Prompt != null)
				items.Add (kAuthorizationEnvironmentPrompt, Prompt);

			if (IconPath != null)
				items.Add (kAuthorizationEnvironmentIcon, IconPath);

			return items;
		}
 	}

	public struct AuthorizationItem
	{
		public string Name { get; }
		public string Value { get; }
		internal int Flags { get; }

		public AuthorizationItem (string name, string value = null)
		{
			Name = name ?? throw new ArgumentNullException (nameof (name));
			Value = value;
			Flags = 0;
		}

		public void Deconstruct (out string name, out string value)
		{
			name = Name;
			value = Value;
		}
	}

	public class AuthorizationItemSet : IReadOnlyList<AuthorizationItem>
	{
		readonly List<AuthorizationItem> items = new List<AuthorizationItem> ();

		public int Count => items.Count;

		public AuthorizationItem this [int index] => items [index];

		public void Add (AuthorizationItem item)
			=> items.Add (item);

		public void Add (string key, string value = null)
			=> items.Add (new AuthorizationItem (key, value));

		public IEnumerator<AuthorizationItem> GetEnumerator ()
			=> items.GetEnumerator ();

		IEnumerator IEnumerable.GetEnumerator ()
			=> GetEnumerator ();

		public override string ToString ()
		{
			var builder = new System.Text.StringBuilder ();
			builder.Append (nameof (AuthorizationItemSet)).AppendLine (" {");
			foreach (var item in this) {
				builder.Append ("  ").Append (item.Name);
				if (item.Value != null)
					builder.Append (" = ").Append (item.Value);
				builder.AppendLine ();
			}
			builder.Append ("}");
			return builder.ToString ();
		}

		internal unsafe void ToNative (ref NativeAuthorizationItemSet native)
		{
			var nativeItems = (NativeAuthorizationItem *)Marshal.AllocHGlobal (
				sizeof (NativeAuthorizationItem) * items.Count);

			native.count = items.Count;
			native.items = nativeItems;

			for (int i = 0; i < native.count; i++) {
				var item = items [i];

				nativeItems [i].flags = item.Flags;
				nativeItems [i].name = Marshal.StringToHGlobalAuto (item.Name);

				if (item.Value == null) {
					nativeItems [i].value = IntPtr.Zero;
					nativeItems [i].valueLen = IntPtr.Zero;
				} else {
					nativeItems [i].value = Marshal.StringToHGlobalAuto (item.Value);
					nativeItems [i].valueLen = (IntPtr)item.Value.Length;
				}
			}
		}

		internal static unsafe void FreeNative (NativeAuthorizationItemSet *itemsPtr)
		{
			if (itemsPtr == null)
				return;

			var items = *itemsPtr;

			if (items.items == null)
				return;

			for (int i = 0; i < items.count; i++) {
				if (items.items [i].name != IntPtr.Zero)
					Marshal.FreeHGlobal (items.items [i].name);

				if (items.items [i].value != IntPtr.Zero)
					Marshal.FreeHGlobal (items.items [i].value);
			}

			Marshal.FreeHGlobal ((IntPtr)items.items);
		}
	}

	public class AuthorizationRights : AuthorizationItemSet
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	struct NativeAuthorizationItem {
		public IntPtr /* AuthorizationString = const char * */ name;
		public IntPtr /* size_t */ valueLen;
		public IntPtr /* void* */ value;
		public int /* UInt32 */ flags;  // zero
	}

	[StructLayout (LayoutKind.Sequential)]
	unsafe struct NativeAuthorizationItemSet {
		public int /* UInt32 */ count;
		public NativeAuthorizationItem * /* AuthorizationItem* */ items;
	}

	public unsafe class Authorization : INativeObject, IDisposable {
		const int kAuthorizationExternalFormLength = 32;

		IntPtr handle;

		public IntPtr Handle { get { return handle; } }
		
		[DllImport (Constants.SecurityLibrary)]
		extern static int /* OSStatus = int */ AuthorizationCreate (NativeAuthorizationItemSet *rights, NativeAuthorizationItemSet *environment, AuthorizationFlags flags, out IntPtr auth);

		[DllImport (Constants.SecurityLibrary)]
		extern static int /* OSStatus = int */ AuthorizationExecuteWithPrivileges (IntPtr handle, string pathToTool, AuthorizationFlags flags, string [] args, IntPtr FILEPtr);

		[DllImport (Constants.SecurityLibrary)]
		static extern int /* OSStatus = int */ AuthorizationMakeExternalForm (IntPtr authorizationRef, IntPtr extForm);

		[DllImport (Constants.SecurityLibrary)]
		extern static int /* OSStatus = int */ AuthorizationFree (IntPtr handle, AuthorizationFlags flags);
		
		internal Authorization (IntPtr handle)
		{
			this.handle = handle;
		}

		[Deprecated (PlatformName.MacOSX, 10, 7)]
		public int ExecuteWithPrivileges (string pathToTool, AuthorizationFlags flags, string [] args)
		{
			return AuthorizationExecuteWithPrivileges (handle, pathToTool, flags, args, IntPtr.Zero);
		}

		public NSData MakeExternalForm ()
		{
			var extForm = Marshal.AllocHGlobal (kAuthorizationExternalFormLength);
			var result = AuthorizationMakeExternalForm (Handle, extForm);
			if (result == 0)
				return NSData.FromBytesNoCopy (
					extForm,
					kAuthorizationExternalFormLength,
					freeWhenDone: true);

			Marshal.FreeHGlobal (extForm);
			throw new Exception ($"AuthorizationMakeExternalForm returned {result}");
		}

		public void Dispose ()
		{
			GC.SuppressFinalize (this);
			Dispose (0, true);
		}

		~Authorization ()
		{
			Dispose (0, false);
		}
		
		public virtual void Dispose (AuthorizationFlags flags, bool disposing)
		{
			if (handle != IntPtr.Zero){
				AuthorizationFree (handle, flags);
				handle = IntPtr.Zero;
			}
		}
		
		public static Authorization Create (AuthorizationFlags flags)
		{
			return Create ((AuthorizationRights)null, null, flags);
		}

		[Obsolete ("Use Create (AuthorizationRights, AuthorizationEnvironment, AuthorizationFlags)")]
		public static Authorization Create (
			AuthorizationParameters parameters,
			AuthorizationEnvironment environment,
			AuthorizationFlags flags)
			=> Create (parameters?.ToAuthorizationRights (), environment, flags);

		public static Authorization Create (
			AuthorizationRights rights,
			AuthorizationEnvironment environment,
			AuthorizationFlags flags)
		{
			NativeAuthorizationItemSet rightsNative = new NativeAuthorizationItemSet ();
			NativeAuthorizationItemSet* rightsPtr = null;

			NativeAuthorizationItemSet envNative = new NativeAuthorizationItemSet ();
			NativeAuthorizationItemSet* envPtr = null;

			int code;
			IntPtr auth;

			try {
				if (rights != null && rights.Count > 0) {
					rights.ToNative (ref rightsNative);
					rightsPtr = &rightsNative;
				}

				if (environment != null) {
					environment.ToAuthorizationItemSet ().ToNative (ref envNative);
					if (envNative.count > 0)
						envPtr = &envNative;
				}

				code = AuthorizationCreate (rightsPtr, envPtr, flags, out auth);
				if (code != 0)
					return null;

				return new Authorization (auth);
			} finally {
				AuthorizationItemSet.FreeNative (rightsPtr);
				AuthorizationItemSet.FreeNative (envPtr);
			}
		}
	}
}

#endif // MONOMAC