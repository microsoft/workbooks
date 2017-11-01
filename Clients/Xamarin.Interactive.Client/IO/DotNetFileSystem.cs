//
// DotNetFileSystem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.IO
{
	abstract class DotNetFileSystem : IFileSystem
	{
		public abstract QuarantineInfo GetQuarantineInfo (FilePath path);
		public abstract void StripQuarantineInfo (FilePath path);

		public virtual FilePath GetTempDirectory (params string [] subdirectories)
		{
			var directory = FilePath
				.GetTempPath ()
				.Combine ("com.xamarin.interactive")
				.Combine (subdirectories);
			directory.CreateDirectory ();
			return directory;
		}
	}
}