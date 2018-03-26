// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    /// <summary>
    /// Represents a parameter of a callable-signature. A parameter can
    /// have a label and a doc-comment.
    /// </summary>
    [MonacoSerializable ("monaco.languages.ParameterInformation")]
    public struct ParameterInformation
    {
        /// <summary>
        /// The label of this signature. Will be shown in the UI.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// The human-readable doc-comment of this signature. Will be shown
        /// in the UI but can be omitted.
        /// </summary>
        [JsonProperty (NullValueHandling = NullValueHandling.Ignore)]
        public string Documentation { get; }

        [JsonConstructor]
        public ParameterInformation (
            string label,
            string documentation = null)
        {
            Label = label ?? throw new ArgumentNullException (nameof (label));
            Documentation = documentation;
        }
    }
}