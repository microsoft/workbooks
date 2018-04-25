// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    /// <summary>
    /// Signature help represents the signature of something
    /// callable. There can be multiple signatures but only one
    /// active and only one active parameter.
    /// </summary>
    [MonacoSerializable ("monaco.languages.SignatureHelp")]
    public struct SignatureHelp
    {
        /// <summary>
        /// One or more signatures.
        /// </summary>
        public IReadOnlyList<SignatureInformation> Signatures { get; }

        /// <summary>
        /// The active signature index.
        /// </summary>
        [JsonProperty (DefaultValueHandling = DefaultValueHandling.Include)]
        public int ActiveSignature { get; }

        /// <summary>
        /// The active parameter index.
        /// </summary>
        [JsonProperty (DefaultValueHandling = DefaultValueHandling.Include)]
        public int ActiveParameter { get; }

        [JsonConstructor]
        public SignatureHelp (
            IReadOnlyList<SignatureInformation> signatures,
            int activeSignature = 0,
            int activeParameter = 0)
        {
            Signatures = signatures ?? Array.Empty<SignatureInformation> ();
            ActiveSignature = 0;
            ActiveParameter = 0;
        }
    }
}