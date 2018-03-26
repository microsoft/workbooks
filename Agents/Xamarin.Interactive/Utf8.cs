//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

namespace Xamarin.Interactive
{
    public static class Utf8
    {
        public static UTF8Encoding Encoding { get; } = new UTF8Encoding (false, false);

        public static byte [] GetBytes (string value)
            => Encoding.GetBytes (value);

        public static string GetString (byte [] bytes, int index, int count)
            => Encoding.GetString (bytes, index, count);

        public static string GetString (byte [] bytes, int count)
            => Encoding.GetString (bytes, 0, count);

        public static string GetString (byte [] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException (nameof (bytes));

            return Encoding.GetString (bytes, 0, bytes.Length);
        }
    }
}