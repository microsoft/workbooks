//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Tests
{
    static class TestHelpers
    {
        public static readonly FilePath PathToRepoRoot;

        static TestHelpers ()
        {
            var assemblyPath = new FilePath (typeof (TestHelpers).Assembly.Location);
            var path = assemblyPath;

            while (path.Exists) {
                if (path.Combine (".git").DirectoryExists) {
                    PathToRepoRoot = path;
                    return;
                }

                path = path.ParentDirectory;
            }

            throw new Exception ($"{assemblyPath} is not in a git repository");
        }

        /// <summary>
		/// Gets a manifest resource stream from the assembly of T.
		/// </summary>
		/// <typeparam name="T">The type to use to determine the assembly from which to read.</typeparam>
        public static Stream GetResource<T> (string resourceId)
            => typeof (T)
                .Assembly
                .GetManifestResourceStream (resourceId);

        /// <summary>
		/// Gets a manifest resource stream from the assembly that provides <see cref="TestHelpers"/>.
		/// </summary>
        public static Stream GetResource (string resourceId)
            => typeof (TestHelpers)
                .Assembly
                .GetManifestResourceStream (resourceId);

        public const string Configuration =
#if DEBUG
			"Debug";
#else
            "Release";
#endif
    }
}
