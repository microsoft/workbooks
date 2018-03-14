//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    public struct EvaluationEnvironment : IEvaluationEnvironment
    {
        public string WorkingDirectory { get; }

        public EvaluationEnvironment (string workingDirectory)
            => WorkingDirectory = workingDirectory;
    }
}