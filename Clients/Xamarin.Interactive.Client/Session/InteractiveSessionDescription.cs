//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Session
{
    public sealed class InteractiveSessionDescription
    {
        public LanguageDescription LanguageDescription { get; }
        public string TargetPlatformIdentifier { get; }
        public EvaluationEnvironment EvaluationEnvironment { get; }

        [JsonConstructor]
        public InteractiveSessionDescription (
            LanguageDescription languageDescription,
            string targetPlatformIdentifier,
            EvaluationEnvironment evaluationEnvironment = default)
        {
            LanguageDescription = languageDescription;
            TargetPlatformIdentifier = targetPlatformIdentifier;
            EvaluationEnvironment = evaluationEnvironment;
        }
    }
}