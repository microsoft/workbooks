// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.IO
{
    class DotNetFileSystem : IFileSystem
    {
        public virtual QuarantineInfo GetQuarantineInfo (FilePath path)
            => null;

        public virtual void StripQuarantineInfo (FilePath path)
        {
        }

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