//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.CodeAnalysis.Events;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    struct CapturedOutputSegment : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public int FileDescriptor { get; }
        public string Value { get; }

        internal CapturedOutputSegment (
            CodeCellId codeCellId,
            int fileDescriptor,
            char [] buffer,
            int index,
            int count)
        {
            CodeCellId = codeCellId;
            FileDescriptor = fileDescriptor;
            Value = new string (buffer, index, count);
        }

        internal CapturedOutputSegment (
            CodeCellId codeCellId,
            int fileDescriptor,
            char singleChar)
        {
            CodeCellId = codeCellId;
            FileDescriptor = fileDescriptor;
            Value = singleChar.ToString ();
        }
    }
}