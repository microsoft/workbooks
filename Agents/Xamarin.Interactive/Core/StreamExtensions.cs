//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Xamarin.Interactive
{
    static class StreamExtensions
    {
        /// <summary>
		/// Only returns when the requested amount of data has actually been read, or
		/// the end of the stream has been reached, unlike Stream.Read which may return
		/// when less data has been read than was requested.
		/// </summary>
        public static int SafeRead (this Stream stream, byte [] buffer, int offset, int count)
        {
            var read = 0;
            do {
                var iterRead = stream.Read (buffer, offset + read, count - read);
                if (iterRead < 1)
                    return read;
                read += iterRead;
            } while (read < count);
            return read;
        }
    }
}