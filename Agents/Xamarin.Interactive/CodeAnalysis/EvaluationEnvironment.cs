//
// EvaluationEnvironment.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    struct EvaluationEnvironment
    {
        public FilePath WorkingDirectory { get; }

        public EvaluationEnvironment (FilePath workingDirectory)
            => WorkingDirectory = workingDirectory;
    }
}