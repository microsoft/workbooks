//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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