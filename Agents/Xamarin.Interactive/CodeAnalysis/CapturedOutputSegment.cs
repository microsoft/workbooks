//
// CapturedOutputSegment.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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