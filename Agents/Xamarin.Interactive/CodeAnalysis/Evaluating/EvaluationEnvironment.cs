//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    [JsonObject]
    public struct EvaluationEnvironment
    {
        public FilePath WorkingDirectory { get; }

        [JsonConstructor]
        public EvaluationEnvironment (FilePath workingDirectory)
            => WorkingDirectory = workingDirectory;
    }
}