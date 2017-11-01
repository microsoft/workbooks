//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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