//
// WindowsFileSystem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.IO.Windows
{
	sealed class WindowsFileSystem : DotNetFileSystem
	{
		public override QuarantineInfo GetQuarantineInfo (FilePath path)
		{
			var zoneInfo = new ZoneInfo (path);
			if (!zoneInfo.IsQuarantined)
				return null;

			return new QuarantineInfo (path);
		}

		public override void StripQuarantineInfo (FilePath path)
			=> new ZoneInfo (path).Unquarantine ();
	}
}