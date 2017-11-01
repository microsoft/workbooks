//
// DumpableStream.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.

using System;
using System.IO;

namespace Xamarin.Interactive.Core
{
    class DumpableStream : Stream
    {
        readonly TextWriter output;
        readonly Stream baseStream;

        public bool DumpRead { get; set; }
        public bool DumpWrite { get; set; }

        public DumpableStream (Stream baseStream) : this (Console.Out, baseStream)
        {
        }

        public DumpableStream (TextWriter output, Stream baseStream)
        {
            if (output == null)
                throw new ArgumentNullException (nameof (output));

            if (baseStream == null)
                throw new ArgumentNullException (nameof (baseStream));

            this.output = output;
            this.baseStream = baseStream;

            DumpRead = true;
            DumpWrite = true;
        }

        public override int Read (byte[] buffer, int offset, int count)
        {
            var read = baseStream.Read (buffer, offset, count);
            if (DumpRead)
                buffer.HexDump (output, " Read: ", offset, count);
            return read;
        }

        public override void Write (byte[] buffer, int offset, int count)
        {
            if (DumpWrite)
                buffer.HexDump (output, "Write: ", offset, count);
            baseStream.Write (buffer, offset, count);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
                baseStream.Dispose ();
        }

        public override void Flush () => baseStream.Flush ();
        public override long Seek (long offset, SeekOrigin origin) => baseStream.Seek (offset, origin);
        public override void SetLength (long value) => baseStream.SetLength (value);

        public override bool CanRead => baseStream.CanRead;
        public override bool CanSeek => baseStream.CanSeek;
        public override bool CanWrite => baseStream.CanWrite;
        public override long Length => baseStream.Length;

        public override long Position {
            get { return baseStream.Position; }
            set { baseStream.Position = value; }
        }
    }
}