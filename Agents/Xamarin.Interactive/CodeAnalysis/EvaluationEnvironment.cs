//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    public struct EvaluationEnvironment
    {
        public FilePath WorkingDirectory { get; }

        public EvaluationEnvironment (FilePath workingDirectory)
            => WorkingDirectory = workingDirectory;
    }
}