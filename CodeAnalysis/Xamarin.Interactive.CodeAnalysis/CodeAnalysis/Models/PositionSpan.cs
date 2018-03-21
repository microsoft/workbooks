// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    /// <summary>
    /// Represents a 1-based span in a buffer with an optional file path.
    /// </summary>
    [MonacoSerializable ("monaco.IRange")]
    public struct PositionSpan
    {
        public int StartLineNumber { get; }
        public int StartColumn { get; }
        public int EndLineNumber { get; }
        public int EndColumn { get; }
        public string FilePath { get; }

        [JsonConstructor]
        public PositionSpan (
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