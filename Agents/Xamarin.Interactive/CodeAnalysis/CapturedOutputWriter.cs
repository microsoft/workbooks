//
// CapturedOutputWriter.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Text;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.CodeAnalysis
{
    sealed class CapturedOutputWriter
    {
        const string TAG = nameof (CapturedOutputWriter);

        internal const int StandardOutputFd = 1;
        internal const int StandardErrorFd = 2;

        readonly object mutex = new object ();
        readonly CodeCellId context;

        public TextWriter StandardOutput { get; }
        public TextWriter StandardError { get; }

        public event Action<CapturedOutputSegment> SegmentCaptured;

        public CapturedOutputWriter (CodeCellId context)
        {
            this.context = context;
            StandardOutput = new Writer (StandardOutputFd, this);
            StandardError = new Writer (StandardErrorFd, this);
        }

        public void NotifySegmentCaptured (CapturedOutputSegment segment)
        {
            lock (mutex) {
                var handlers = SegmentCaptured?.GetInvocationList ();
                if (handlers == null)
                    return;

                foreach (Action<CapturedOutputSegment> handler in handlers) {
                    try {
                        handler.Invoke (segment);
                    } catch (Exception e) {
                        Log.Error (TAG, $"bad SegmentCaptured handler ({handler})", e);
                    }
                }
            }
        }

        sealed class Writer : TextWriter
        {
            readonly int fileDescriptor;
            readonly CapturedOutputWriter monitor;
            readonly CodeCellId context;

            public override Encoding Encoding => Utf8.Encoding;

            public Writer (int fileDescriptor, CapturedOutputWriter monitor)
            {
                this.fileDescriptor = fileDescriptor;
                this.monitor = monitor;
                this.context = monitor.context;
            }

            public override void Write (char value)
                => monitor.NotifySegmentCaptured (
                    new CapturedOutputSegment (fileDescriptor, value, context));

            public override void Write (char [] buffer, int index, int count)
                => monitor.NotifySegmentCaptured (
                    new CapturedOutputSegment (fileDescriptor, buffer, index, count, context));
        }
    }
}