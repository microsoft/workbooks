// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    /// <summary>
    /// Represents the signature of something callable. A signature
    /// can have a label, like a function-name, a doc-comment, and
    /// a set of parameters.
    /// </summary>
    [MonacoSerializable ("monaco.languages.SignatureInformation")]
    public struct SignatureInformation
    {
        /// <summary>
        /// The label of this signature. Will be shown in the UI.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// The parameters of this signature.
        /// </summary>
        public IReadOnlyList<ParameterInformation> Parameters { get; }

        /// <summary>
        /// The human-readable doc-comment of this signature.
        /// Will be shown in the UI but can be omitted.
        /// </summary>
        [JsonProperty (NullValueHandling = NullValueHandling.Ignore)]
        public string Documentation { get; }

        [JsonConstructor]
        public SignatureInformation (
            string label,
            IReadOnlyList<ParameterInformation> parameters = null,
            string documentation = null)
        {
            Label = label ?? throw new ArgumentNullException (nameof (label));
            Parameters = parameters ?? Array.Empty<ParameterInformation> ();
            Documentation = documentation;
        }
    }
}