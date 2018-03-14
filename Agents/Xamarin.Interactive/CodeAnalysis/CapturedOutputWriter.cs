//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        readonly CodeCellId codeCellId;

        public TextWriter StandardOutput { get; }
        public TextWriter StandardError { get; }

        public event Action<CapturedOutputSegment> SegmentCaptured;

        public CapturedOutputWriter (CodeCellId codeCellId)
        {
            this.codeCellId = codeCellId;
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
            readonly CodeCellId codeCellId;

            public override Encoding Encoding => Utf8.Encoding;

            public Writer (int fileDescriptor, CapturedOutputWriter monitor)
            {
                this.fileDescriptor = fileDescriptor;
                this.monitor = monitor;
                this.codeCellId = monitor.codeCellId;
            }

            public override void Write (char value)
                => monitor.NotifySegmentCaptured (
                    new CapturedOutputSegment (codeCellId, fileDescriptor, value));

            public override void Write (char [] buffer, int index, int count)
                => monitor.NotifySegmentCaptured (
                    new CapturedOutputSegment (codeCellId, fileDescriptor, buffer, index, count));
        }
    }
}