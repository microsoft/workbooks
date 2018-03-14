//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

namespace Xamarin.Interactive
{
    static class DevEnvironment
    {
        public static string RepositoryRootDirectory { get; }

#if DEBUG

        static DevEnvironment ()
        {
            var path = Path.GetDirectoryName (typeof (DevEnvironment).Assembly.Location);
            while (Directory.Exists (path)) {
                if (Directory.Exists (Path.Combine (path, ".git"))) {
                    RepositoryRootDirectory = path;
                    break;
                }

                path = Path.GetDirectoryName (path);
            }
        }

#endif
    }
}