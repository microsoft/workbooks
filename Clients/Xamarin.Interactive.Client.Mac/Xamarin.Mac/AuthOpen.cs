//
// AuthOpen.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Foundation;

namespace Security
{
	using static XamCore.Security.AuthorizationFlags;

	sealed class AuthOpen
	{
		public static AuthOpen Preauthorize (
			XamCore.Security.AuthorizationEnvironment environment,
			string path,
			uint mode = 0,
			bool create = true)
		{
			if (path == null)
				throw new ArgumentNullException (nameof (path));

			return new AuthOpen (
				XamCore.Security.Authorization.Create (
					new XamCore.Security.AuthorizationRights {
						{ "sys.openfile.readwrite" + (create ? "create" : "") + "." + path }
					},
					environment,
					InteractionAllowed | ExtendRights | PreAuthorize),
				path,
				mode,
				create);
		}

		public static Task<AuthOpen> PreauthorizeAsync (
			XamCore.Security.AuthorizationEnvironment environment,
			string path,
			uint mode = 0,
			bool create = true)
		{
			if (path == null)
				throw new ArgumentNullException (nameof (path));

			return Task.Run (() => Preauthorize (environment, path, mode, create));
		}

		public XamCore.Security.Authorization Authorization { get; }
		public string Path { get; }
		public uint Mode { get; }
		public bool Create { get; }

		public AuthOpen (XamCore.Security.Authorization authorization, string path, uint mode = 0, bool create = true)
		{
			Authorization = authorization
				?? throw new ArgumentNullException (nameof (authorization));

			Path = path
				?? throw new ArgumentNullException (nameof (path));

			Mode = mode;
			Create = create;
		}

		public void Copy (string sourcePath)
		{
			if (sourcePath == null)
				throw new ArgumentNullException (nameof (sourcePath));

			Write (file => file.WriteData (NSData.FromFile (sourcePath)));
		}

		public Task CopyAsync (string sourcePath)
		{
			if (sourcePath == null)
				throw new ArgumentNullException (nameof (sourcePath));

			return Task.Run (() => Copy (sourcePath));
		}

		public Task WriteAsync (Action<NSFileHandle> writeHandler)
		{
			if (writeHandler == null)
				throw new ArgumentNullException (nameof (writeHandler));

			return Task.Run (() => Write (writeHandler)); 
		}

		public void Write (Action<NSFileHandle> writeHandler)
		{
			if  (writeHandler == null)
				throw new ArgumentNullException (nameof (writeHandler));

			var args = new List<string> {
				"-extauth"
			};

			if  (Create)
				args.Add ("-c");

			if (Mode > 0) {
				args.Add ("-m");
				args.Add ("0" + Convert.ToString (Mode, 8));
			}

			args.Add ("-w");
			args.Add (Path);

			var task = new NSTask {
				LaunchPath = "/usr/libexec/authopen",
				Arguments = args.ToArray ()
			};

			var stdinPipe = new NSPipe ();
			var stderrPipe = new NSPipe ();
			task.StandardInput = stdinPipe;
			task.StandardError = stderrPipe;

			try {
				task.Launch ();

				stdinPipe.WriteHandle.WriteData (Authorization.MakeExternalForm ());
				writeHandler (stdinPipe.WriteHandle);
			} finally {
				stdinPipe.WriteHandle.CloseFile ();
			}

			task.WaitUntilExit ();

			try {
				if (task.TerminationReason == NSTaskTerminationReason.Exit &&
					task.TerminationStatus == 0)
					return;

				var errorMessage = stderrPipe
					.ReadHandle
					.ReadDataToEndOfFile ()
					.ToString ()
					.Trim ();

				if (string.IsNullOrEmpty (errorMessage))
					errorMessage =
						$"authopen failed with termination reason {task.TerminationReason} " +
						$"and termination status {task.TerminationStatus}";

				throw new Exception (errorMessage);
			} finally {
				stderrPipe.ReadHandle.CloseFile ();
			}
		}
	}
}