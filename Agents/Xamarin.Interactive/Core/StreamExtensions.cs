//
// StreamExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014 Xamarin Inc. All rights reserved.

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