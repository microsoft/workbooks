//
// MonoRuntimeVersion.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Runtime.InteropServices;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
	public sealed class MonoRuntimeVersion : Task
	{
		[DllImport ("libc")]
		static extern IntPtr dlopen (string path, int mode);

		[DllImport ("libc")]
		static extern IntPtr dlsym (IntPtr handle, string symbol);

		delegate string mono_get_runtime_build_info ();
		const string mono_get_runtime_build_info_name = nameof (mono_get_runtime_build_info);

		static Version GetLibMonoVersion (string libMonoPath, bool majorMinorOnly)
		{
			var handle = dlopen (libMonoPath, 0);
			if (handle == IntPtr.Zero)
				throw new Exception ($"unable to dlopen {libMonoPath}");

			var sym = dlsym (handle, mono_get_runtime_build_info_name);
			if (sym == IntPtr.Zero)
				throw new Exception ($"cannot load {mono_get_runtime_build_info_name} " +
					$"from {libMonoPath}");

			var del = (mono_get_runtime_build_info)Marshal.GetDelegateForFunctionPointer (
				sym, typeof (mono_get_runtime_build_info));
			if (del == null)
				throw new Exception ($"cannot bind delegate to " +
					$"{mono_get_runtime_build_info_name} from {libMonoPath}");

			var buildInfo = del ();
			if (buildInfo == null)
				return null;

			var version = new Version (buildInfo.Substring (0, buildInfo.IndexOf (' ')));
			if (majorMinorOnly)
				version = new Version (version.Major, version.Minor);
			return version;
		}

		[Required]
		public string LibMonoPathA { get; set; }

		public string LibMonoPathB { get; set; }

		public bool MajorMinorOnly { get; set; }

		public bool ErrorIfNotEqual { get; set; }

		[Output]
		public string VersionA { get; private set; }

		[Output]
		public string VersionB { get; private set; }

		[Output]
		public int Comparison { get; private set; }

		[Output]
		public bool AreEqual => Comparison == 0;

		public override bool Execute ()
		{
			VersionA = null;
			VersionB = null;
			Comparison = -2;

			try {
				var versionA = GetLibMonoVersion (LibMonoPathA, MajorMinorOnly);
				VersionA = versionA.ToString ();

				Log.LogMessage ($"{LibMonoPathA} version {VersionA}");

				if (String.IsNullOrEmpty (LibMonoPathB))
					return true;

				var versionB = GetLibMonoVersion (LibMonoPathB, MajorMinorOnly);
				VersionB = versionB.ToString ();

				Log.LogMessage ($"{LibMonoPathB} version {VersionB}");

				Comparison = versionA.CompareTo (versionB);

				if (ErrorIfNotEqual && !AreEqual) {
					Log.LogError (
						"Mono runtime versions do not match. " +
						$"{LibMonoPathA} is {VersionA} and {LibMonoPathB} is {VersionB}");
					return false;
				}

				return true;
			} catch (Exception e) {
				Log.LogErrorFromException (e, false);
				return false;
			}
		}
	}
}