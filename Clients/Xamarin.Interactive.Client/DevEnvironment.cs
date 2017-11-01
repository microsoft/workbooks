//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive
{
    static class DevEnvironment
    {
        public static FilePath RepositoryRootDirectory { get; }

#if DEBUG

        static DevEnvironment ()
        {
            var path = new FilePath (typeof (DevEnvironment).Assembly.Location);
            while (path.Exists) {
                if (path.Combine (".git").DirectoryExists) {
                    RepositoryRootDirectory = path;
                    break;
                }

                path = path.ParentDirectory;
            }
        }

#endif
    }
}