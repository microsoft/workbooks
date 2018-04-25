// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    /// <summary>
    /// Represents a 1-based span in a buffer with an optional file path.
    /// </summary>
    [MonacoSerializable ("monaco.IRange")]
    public struct Range
    {
        [JsonProperty (DefaultValueHandling = DefaultValueHandling.Include)]
        public int StartLineNumber { get; }

        [JsonProperty (DefaultValueHandling = DefaultValueHandling.Include)]
        public int StartColumn { get; }

        [JsonProperty (DefaultValueHandling = DefaultValueHandling.Include)]
        public int EndLineNumber { get; }

        [JsonProperty (DefaultValueHandling = DefaultValueHandling.Include)]
        public int EndColumn { get; }

        [JsonProperty (DefaultValueHandling = DefaultValueHandling.Include)]
        public string FilePath { get; }

        [JsonConstructor]
        public Range (
            int startLineNumber,
            int startColumn,
            int endLineNumber,
            int endColumn,
            string filePath = null)
        {
            StartLineNumber = startLineNumber;
            StartColumn = startColumn;
            EndLineNumber = endLineNumber;
            EndColumn = endColumn;
            FilePath = filePath;
        }

        public void Deconstruct (
            out int startLineNumber,
            out int startColumn)
        {
            startLineNumber = StartLineNumber;
            startColumn = StartColumn;
        }

        public void Deconstruct (
            out int startLineNumber,
            out int startColumn,
            out int endLineNumber,
            out int endColumn)
        {
            startLineNumber = StartLineNumber;
            startColumn = StartColumn;
            endLineNumber = EndLineNumber;
            endColumn = EndColumn;
        }
    }
}