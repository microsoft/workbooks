//
// IFileSystem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.IO
{
	interface IFileSystem
	{
		QuarantineInfo GetQuarantineInfo (FilePath path);
		void StripQuarantineInfo (FilePath path);

		FilePath GetTempDirectory (params string [] subdirectories);
	}
}