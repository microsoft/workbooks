//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Xamarin.ProcessControl
{
    public sealed class ConsoleRedirection
    {
        public enum FileDescriptor
        {
            None,
            Output,
            Error
        }

        public sealed class SegmentCollection : INotifyCollectionChanged
        {
            readonly ObservableCollection<Segment> segments = new ObservableCollection<Segment> ();

            public event NotifyCollectionChangedEventHandler CollectionChanged {
                add => segments.CollectionChanged += value;
                remove => segments.CollectionChanged -= value;
            }

            public void Add (Segment segment)
            {
                lock (segments)
                    segments.Add (segment);
            }

            public void Write (TextWriter writer, FileDescriptor fileDescriptor = FileDescriptor.None)
            {
                lock (segments) {
                    foreach (var segment in segments) {
                        if (fileDescriptor == FileDescriptor.None ||
                            segment.FileDescriptor == fileDescriptor)
                            writer.Write (segment.Data);
                    }
                }
            }

            public override string ToString () => ToString (FileDescriptor.None);

            public string ToString (FileDescriptor fileDescriptor)
            {
                using (var writer = new StringWriter ()) {
                    Write (writer, fileDescriptor);
                    return writer.ToString ();
                }
            }
        }

        [Serializable]
        public struct Segment
        {
            public FileDescriptor FileDescriptor { get; }
            public string Data { get; }

            internal Segment (FileDescriptor fileDescriptor, char [] buffer, int index, int count)
            {
                FileDescriptor = fileDescriptor;
                Data = new string (buffer, index, count);
            }

            internal Segment (FileDescriptor fileDescriptor, char singleChar)
            {
                FileDescriptor = fileDescriptor;
                Data = singleChar.ToString ();
            }

            public void Deconstruct (out FileDescriptor fd, out string data)
            {
                fd = FileDescriptor;
                data = Data;
            }
        }

        public Action<Segment> WriteHandler { get; }
        public TextWriter StandardOutput { get; }
        public TextWriter StandardError { get; }

        public ConsoleRedirection (Action<Segment> writeHandler)
        {
            WriteHandler = writeHandler ?? throw new ArgumentNullException (nameof (writeHandler));

            StandardOutput = new Writer (FileDescriptor.Output, this);
            StandardError = new Writer (FileDescriptor.Error, this);
        }

        sealed class Writer : TextWriter
        {
            readonly FileDescriptor fileDescriptor;
            readonly ConsoleRedirection redirection;

            public override Encoding Encoding { get; } = new UTF8Encoding (false, false);

            public Writer (FileDescriptor fileDescriptor, ConsoleRedirection redirection)
            {
                this.fileDescriptor = fileDescriptor;
                this.redirection = redirection;
            }

            public override void Write (char value)
                => redirection?.WriteHandler (
                    new Segment (fileDescriptor, value));

            public override void Write (char [] buffer, int index, int count)
                => redirection?.WriteHandler (
                    new Segment (fileDescriptor, buffer, index, count));
        }
    }
}