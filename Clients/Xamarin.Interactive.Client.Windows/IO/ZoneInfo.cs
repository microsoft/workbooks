//
// ZoneInfo.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Microsoft.Win32.SafeHandles;

namespace Xamarin.Interactive.IO.Windows
{
	sealed class ZoneInfo
	{
		const string ZoneIdentifierStreamName = ":Zone.Identifier";

		const int ErrorFileNotFound = 2;

		/// <summary>
		/// The ZoneId specified in the Zone.Identifier alternate data stream
		/// if it exists 0 otherwise
		/// </summary>
		int ZoneId { get; }

		/// <summary>
		/// Is the file considered quarantined
		/// </summary>
		public bool IsQuarantined => ZoneId > 0;

		public string AdsPath { get; }

		public ZoneInfo (string path)
		{
			if (path == ".")
				return;

			AdsPath = path + ZoneIdentifierStreamName;

			// Work around the .Net behavior of not allowing access to ntfs alternate data streams by
			// calling CreateFile directly
			using (var handle = CreateFile (AdsPath, FileAccess.Read,
				FileShare.Read, IntPtr.Zero, FileMode.Open,
				FileAttributes.Normal, IntPtr.Zero)) {

				if (handle.IsInvalid) {
					var error = Marshal.GetLastWin32Error ();

					if (error != ErrorFileNotFound)
						throw new Win32Exception (error);

					return;
				}

				using (var reader = new StreamReader (new FileStream (handle,
					FileAccess.Read))) {
					// The alternate data stream looks like it probably an .ini file but
					// nothing seems to indicate there is ever any other data than the
					// ZoneTransfer section so instead of using GetPrivateProfileString
					// just check for what we expect
					var section = reader.ReadLine ();
					var zone = reader.ReadLine ();

					if (section != "[ZoneTransfer]" || zone == null || !Regex.IsMatch (zone, @"(?:App)?ZoneId=([0-9])"))
						throw new ApplicationException ($"Badly formed {ZoneIdentifierStreamName} alternate data stream on {path}");

					var vals = zone.Split ('=');

					int id;
					if (vals.Length != 2 || !Int32.TryParse (vals[1], out id))
						throw new ApplicationException ($"Badly formed {ZoneIdentifierStreamName} alternate data stream on {path}");

					if (vals[0] == "AppZoneId")
						return;

					ZoneId = id;
				}
			}
		}

		public void Unquarantine ()
		{
			if (!IsQuarantined)
				return;

			using (var handle = CreateFile (AdsPath, FileAccess.Write,
				FileShare.None, IntPtr.Zero, FileMode.Open,
				FileAttributes.Normal, IntPtr.Zero)) {

				if (handle.IsInvalid) {
					var error = Marshal.GetLastWin32Error ();

					if (error != ErrorFileNotFound)
						throw new Win32Exception (error);

					return;
				}

				using (var stream = new FileStream (handle, FileAccess.Write)) {
					stream.SetLength (0);

					using (var writer = new StreamWriter (stream)) {
						writer.WriteLine ("[ZoneTransfer]");
						writer.WriteLine ($"AppZoneId={ZoneId}");
						writer.WriteLine ("");
						writer.Close ();
					}
				}
			}
		}

		public void RemoveInfo ()
		{
			// invoke DeleteFile directly on the Zone.Identifier ADS
			if (!DeleteFile (AdsPath))
				throw new Win32Exception (Marshal.GetLastWin32Error ());
		}

		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern SafeFileHandle CreateFile (
			string name,
			FileAccess access,
			FileShare share,
			IntPtr security,
			FileMode mode,
			FileAttributes flags,
			IntPtr template);

		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs (UnmanagedType.Bool)]
		static extern bool DeleteFile (string name);

	}
}
