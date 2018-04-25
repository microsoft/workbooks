//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis.Events;

namespace Xamarin.Interactive.CodeAnalysis
{
    [JsonObject]
    struct CapturedOutputSegment : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public int FileDescriptor { get; }
        public string Value { get; }

        [JsonConstructor]
        CapturedOutputSegment (
            CodeCellId codeCellId,
            int fileDescriptor,
            string value)
        {
            CodeCellId = codeCellId;
            FileDescriptor = fileDescriptor;
            Value = value;
        }

        internal CapturedOutputSegment (
            CodeCellId codeCellId,
            int fileDescriptor,
            char [] buffer,
            int index,
            int count) : this (
            codeCellId,
            fileDescriptor,
            new string (buffer, index, count))
        {
        }

        internal CapturedOutputSegment (
            CodeCellId codeCellId,
            int fileDescriptor,
            char singleChar) : this (
            codeCellId,
            fileDescriptor,
            singleChar.ToString ())
        {
        }
    }
}