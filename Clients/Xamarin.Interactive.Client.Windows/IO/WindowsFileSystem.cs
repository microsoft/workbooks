//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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