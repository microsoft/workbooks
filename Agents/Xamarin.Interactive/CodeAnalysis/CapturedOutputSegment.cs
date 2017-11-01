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
    struct CapturedOutputSegment
    {
        public int FileDescriptor { get; }
        public string Value { get; }
        public CodeCellId Context { get; }

        internal CapturedOutputSegment (int fileDescriptor, char [] buffer, int index, int count, CodeCellId context)
        {
            FileDescriptor = fileDescriptor;
            Value = new string (buffer, index, count);
            Context = context;
        }

        internal CapturedOutputSegment (int fileDescriptor, char singleChar, CodeCellId context)
        {
            FileDescriptor = fileDescriptor;
            Value = singleChar.ToString ();
            Context = context;
        }
    }
}