//
// PropertyList.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2009 Novell, Inc.
// Copyright 2013 Xamarin, Inc.
// Copyright 2018 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Xamarin.Interactive.Collections.PropertyList
{
    #region Property List Data Types

    sealed class PlistDictionary : KeyedCollection<string, KeyValuePair<string, object>>
    {
        public static PlistDictionary Load (string file)
        {
            using (var stream = File.OpenRead (file))
                return (PlistDictionary)PlistParser.Parse (stream);
        }

        public bool TryGetValue<T> (string key, out T value)
        {
            if (Contains (key)) {
                value = Get<T> (key);
                return true;
            }

            value = default (T);
            return false;
        }

        public T Get<T> (string key)
            => (T)this [key];

        public new object this [string key] {
            get {
                try {
                    return base [key].Value;
                } catch (KeyNotFoundException) {
                    throw new KeyNotFoundException (key);
                }
            }
        }

        public void Add (string key, object value)
            => Add (new KeyValuePair<string, object> (key, value));

        protected override string GetKeyForItem (KeyValuePair<string, object> entry)
            => entry.Key;
    }

    sealed class PlistArray : List<object>
    {
    }

    sealed class PlistSet : HashSet<object>
    {
    }

    static class PlistDate
    {
        static readonly DateTime refDate = new DateTime (2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        const string iso8601Format = @"yyyy-MM-dd\THH:mm:ss\Z";

        public static DateTime FromAbsoluteTime (double absoluteTime)
            => refDate + TimeSpan.FromSeconds (absoluteTime);

        public static DateTime FromString (string iso8601)
            => DateTime.ParseExact (iso8601, iso8601Format, CultureInfo.InvariantCulture);

        public static string ToString (DateTime dateTime)
            => dateTime.ToString (iso8601Format, CultureInfo.InvariantCulture);

        public static double ToAbsoluteTime (DateTime dateTime)
            => (dateTime - refDate).TotalSeconds;
    }

    #endregion

    #region Property List Parsers

    static class PlistParser
    {
        public static object Parse (Stream stream)
        {
            var ms = new MemoryStream ();
            stream.CopyTo (ms);
            ms.Seek (0, SeekOrigin.Begin);

            var xml = (char)ms.ReadByte () == '<';

            ms.Seek (0, SeekOrigin.Begin);

            if (xml)
                return new PlistXmlParser (ms).Parse ();

            return new PlistBinaryParser (ms).Parse ();
        }
    }

    sealed class PlistBinaryParser
    {
        const int TRAILER_SIZE = 5 + 1 + 1 + 1 + 8 + 8 + 8;

        struct Trailer
        {
            public byte SortVersion { get; }
            public byte OffsetIntSize { get; }
            public byte ObjectRefSize { get; }
            public ulong NumObjects { get; }
            public ulong TopObject { get; }
            public ulong OffsetTableOffset { get; }

            public Trailer (
                byte sortVersion,
                byte offsetIntSize,
                byte objectRefSize,
                ulong numObjects,
                ulong topObject,
                ulong offsetTableOffset)
            {
                SortVersion = sortVersion;
                OffsetIntSize = offsetIntSize;
                ObjectRefSize = objectRefSize;
                NumObjects = numObjects;
                TopObject = topObject;
                OffsetTableOffset = offsetTableOffset;
            }

            public override string ToString ()
                => string.Format (
                    "SortVersion: {0}, OffsetIntSize: {1}, ObjectRefSize: {2}, " +
                    "NumObjects: {3}, TopObject: {4}, OffsetTableOffset: {5}",
                    SortVersion, OffsetIntSize, ObjectRefSize,
                    NumObjects, TopObject, OffsetTableOffset);
        }

        enum MarkerKind {
            Null = 0x00,
            False = 0x08,
            True = 0x09,
            Fill = 0x0F,
            Int = 0x10,
            Real = 0x20,
            Date = 0x33,
            Data = 0x40,
            AsciiString = 0x50,
            Unicode16String = 0x60,
            Uid = 0x80,
            Array = 0xA0,
            Set = 0xC0,
            Dict = 0xD0
        }

        readonly Stream stream;
        readonly BinaryReader reader;

        Trailer trailer;
        ulong [] offsetTable;

        public PlistBinaryParser (Stream stream)
        {
            this.stream = stream;
            this.reader = new BinaryReader (stream);
        }

        public object Parse ()
        {
            if (stream.Length == 0)
                return null;

            ParseHeader ();
            ParseTrailer ();
            ValidateTrailer ();
            ParseOffsetTable ();
            return ParseObject (trailer.TopObject);
        }

        void ParseHeader ()
        {
            var header = Swap (reader.ReadUInt64 ());

            if (header >> 16 != 0x62706C697374)
                Fatal ("magic 'bplist' expected as header");

            switch (header & 0x000000000000FFFF) {
            case 0x3030:
                break;
            default:
                Fatal ("expected '00' as version");
                break;
            }
        }

        void ParseTrailer ()
        {
            stream.Seek (stream.Length - TRAILER_SIZE + 5, SeekOrigin.Begin);

            trailer = new Trailer (
                sortVersion: reader.ReadByte (),
                offsetIntSize: reader.ReadByte (),
                objectRefSize: reader.ReadByte (),
                numObjects: Swap (reader.ReadUInt64 ()),
                topObject: Swap (reader.ReadUInt64 ()),
                offsetTableOffset: Swap (reader.ReadUInt64 ()));
        }

        void ParseOffsetTable ()
        {
            Seek (trailer.OffsetTableOffset);

            offsetTable = new ulong [trailer.NumObjects];
            for (var i = 0UL; i < trailer.NumObjects; i++)
                offsetTable [i] = ReadSizedInt (trailer.OffsetIntSize);
        }

        object ParseObject (ulong objectIndex)
        {
            Seek (offsetTable [objectIndex]);

            var header = reader.ReadByte ();
            var kind = (MarkerKind)(header & 0xF0);
            var subKind = (MarkerKind)(header);
            var length = header & 0x0F;

            switch ((MarkerKind)header) {
            case MarkerKind.Date:
                return PlistDate.FromAbsoluteTime (ReadSizedDouble (8));
            }

            switch (kind) {
            case MarkerKind.Data:
            case MarkerKind.AsciiString:
            case MarkerKind.Unicode16String:
            case MarkerKind.Array:
            case MarkerKind.Set:
            case MarkerKind.Dict:
                if (length == 0xF)
                    length = (int)ReadInt ();
                break;
            }

            switch (kind) {
            case MarkerKind.Null:
                switch (subKind) {
                case MarkerKind.Null:
                case MarkerKind.Fill:
                    return null;
                case MarkerKind.False:
                    return false;
                case MarkerKind.True:
                    return true;
                }
                break;
            case MarkerKind.Uid:
                var buffer = new byte [16];
                reader.Read (buffer, 0, length + 1);
                return new Guid (buffer);
            case MarkerKind.Int:
                return (long)ReadSizedInt (1UL << length);
            case MarkerKind.Real:
                return ReadSizedDouble (1UL << length);
            case MarkerKind.Data:
                return reader.ReadBytes (length);
            case MarkerKind.AsciiString:
                var strBytes = reader.ReadBytes (length);
                return Encoding.ASCII.GetString (strBytes);
            case MarkerKind.Unicode16String:
                var uniBytes = reader.ReadBytes (length);
                return Encoding.BigEndianUnicode.GetString (uniBytes);
            case MarkerKind.Array:
            case MarkerKind.Set:
                ICollection<object> array;
                if (kind == MarkerKind.Array)
                    array = new PlistArray ();
                else
                    array = new PlistSet ();

                var arrayStart = (ulong)stream.Position;

                for (var i = 0UL; i < (ulong)length; i++) {
                    Seek (arrayStart + i * trailer.ObjectRefSize);
                    var valueRef = ReadSizedInt (trailer.ObjectRefSize);
                    array.Add (ParseObject (valueRef));
                }

                return (object)array;
            case MarkerKind.Dict:
                var dict = new PlistDictionary ();
                var dictStart = (ulong)stream.Position;

                for (var i = 0UL; i < (ulong)length; i++) {
                    Seek (dictStart + i * trailer.ObjectRefSize);
                    var keyRef = ReadSizedInt (trailer.ObjectRefSize);

                    Seek (dictStart + i * trailer.ObjectRefSize + (ulong)length * trailer.ObjectRefSize);
                    var valueRef = ReadSizedInt (trailer.ObjectRefSize);

                    dict.Add ((string)ParseObject (keyRef), ParseObject (valueRef));
                }

                return dict;
            }

            Fatal ("Unhandled object kind: {0}", kind);
            return null;
        }

        void ValidateTrailer ()
        {
            // Don't overflow on number of objects or offset of the table
            if (trailer.NumObjects >= Int64.MaxValue)
                Fatal ("trailer.NumObjects >= Int64.MaxValue");
            else if (trailer.OffsetTableOffset >= Int64.MaxValue)
                Fatal ("trailer.OffsetTableOffset >= Int64.MaxValue)");

            // Must be a minimum of one object
            if (trailer.NumObjects < 1)
                Fatal ("trailer.NumObjects < 1");

            // Ensure the top object is in range
            if (trailer.NumObjects <= trailer.TopObject)
                Fatal ("trailer.NumObjects <= trailer.TopObject");

            // The offset table must be at least 9 bytes into the file
            // ('bplist??' + 1 byte of object table data)
            if (trailer.OffsetTableOffset < 9)
                Fatal ("trailer.OffsetTableOffset < 9");

            // The trailer must point to a value before itself in the file
            if ((uint)(stream.Length - TRAILER_SIZE) <= trailer.OffsetTableOffset)
                Fatal ("stream.Length - TRAILER_SIZE <= trailer.OffsetTableOffset");

            // Minimum of 1 byte for the size of integers and refs in the file
            if (trailer.OffsetIntSize < 1)
                Fatal ("trailer.OffsetIntSize < 1");
            else if (trailer.ObjectRefSize < 1)
                Fatal ("trailer.ObjectRefSize < 1");

            // Total size of offset table must not overflow
            var offsetTableSize = checked (trailer.NumObjects * trailer.OffsetIntSize);

            // Offset table must have at least one entry
            if (offsetTableSize < 1)
                Fatal ("trailer offsetTableSize < 1");

            // Ensure the size of the offset table and data sections do not overflow
            var objectDataSize = trailer.OffsetTableOffset - 8;
            var tmp = checked (objectDataSize + 8);
            tmp = checked (tmp + offsetTableSize);
            tmp = checked (tmp + TRAILER_SIZE);

            // The total size of the file should be equal to offsetTableOffset + TRAILER_SIZE
            if ((ulong)stream.Length != tmp)
                Fatal ("stream.Length != trailer.OffsetTableOffset + TRAILER_SIZE");

            // The object refs must be the right size to point into the offset table.
            // That is, if the count of objects is 260, but only 1 byte is used to
            // store references (max value 255), something is wrong.
            if (trailer.ObjectRefSize < 8 && (1UL << (8 * trailer.ObjectRefSize)) <= trailer.NumObjects)
                Fatal ("trailer.ObjectRefSize invalid");

            // The integers used for pointers in the offset table must be able to
            // reach as far as the start of the offset table.
            if (trailer.OffsetIntSize < 8 && (1UL << (8 * trailer.OffsetIntSize)) <= trailer.OffsetTableOffset)
                Fatal ("trailer.OffsetIntSize invalid");

            Seek (trailer.OffsetTableOffset + trailer.TopObject * trailer.OffsetIntSize);
            var ofs = ReadSizedInt (trailer.OffsetIntSize);
            if (ofs < 8 || trailer.OffsetTableOffset <= ofs)
                Fatal ("offset ({0}) < 8 || trailer.OffsetTableOffset <= offset ({0})", ofs);
        }

        #region Utilities

        static void Fatal (string message, params object [] args)
            => throw new Exception (String.Format (message, args));

        static ushort Swap (ushort n)
            => (ushort)(((n >> 8) & 0xFF) | ((n << 8) & 0xFF00));

        static uint Swap (uint n)
        {
            return
                ((n >> 24) & 0xFF) |
                ((n >> 08) & 0xFF00) |
                ((n << 08) & 0xFF0000) |
                ((n << 24));
        }

        static ulong Swap (ulong n)
        {
            return
                ((n >> 56) & 0xFF) |
                ((n >> 40) & 0xFF00) |
                ((n >> 24) & 0xFF0000) |
                ((n >> 08) & 0xFF000000) |
                ((n << 08) & 0xFF00000000) |
                ((n << 24) & 0xFF0000000000) |
                ((n << 40) & 0xFF000000000000) |
                (n << 56);
        }

        void Seek (ulong offset, SeekOrigin seekOrigin = SeekOrigin.Begin)
            => stream.Seek ((long)offset, seekOrigin);

        ulong ReadInt ()
        {
            var marker = reader.ReadByte ();
            if ((marker & 0xF0) != (int)MarkerKind.Int)
                Fatal ("marker is not an int");

            var count = 1 << (marker & 0x0F);
            return ReadSizedInt ((ulong)count);
        }

        ulong ReadSizedInt (ulong size)
        {
            switch (size) {
            case 1:
                return (ulong)reader.ReadByte ();
            case 2:
                return (ulong)Swap (reader.ReadUInt16 ());
            case 4:
                return (uint)Swap (reader.ReadUInt32 ());
            case 8:
                return Swap (reader.ReadUInt64 ());
            default:
                var n = 0UL;
                for (var i = 0UL; i < size; i++) {
                    n <<= 8;
                    n |= (uint)(reader.ReadByte () & 0xFF);
                }
                return n;
            }
        }

        double ReadSizedDouble (ulong size)
            => BitConverter.Int64BitsToDouble ((long)ReadSizedInt (size));

        #endregion
    }

    // FIXME: this really should probably use XmlReader directly,
    // as the XLINQ code is super slow. The binary version of
    // the same plist is about 5 seconds faster to parse.
    sealed class PlistXmlParser
    {
        readonly Stream stream;

        public PlistXmlParser (Stream stream)
            => this.stream = stream;

        public object Parse ()
        {
            var reader = new XmlTextReader (stream) {
                DtdProcessing = DtdProcessing.Ignore
            };

            var doc = XDocument.Load (reader);

            var plist = doc.Root;

            if (plist.Name.LocalName.ToLower () != "plist")
                throw new Exception ("not a <plist> document");

            return ParseObject (plist.Descendants ().First ());
        }

        object ParseObject (XElement element)
        {
            var elementName = element.Name.LocalName;

            switch (elementName.ToLowerInvariant ()) {
            case "true":
                return true;
            case "false":
                return false;
            case "integer":
                long l;
                if (!long.TryParse (element.Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out l))
                    l = (long)ulong.Parse (element.Value,
                        NumberStyles.Integer, CultureInfo.InvariantCulture);
                return l;
            case "real":
                return double.Parse (element.Value, CultureInfo.InvariantCulture);
            case "string":
                return element.Value;
            case "data":
                return Convert.FromBase64String (element.Value);
            case "date":
                return PlistDate.FromString (element.Value);
            case "array":
                var arr = new PlistArray ();
                foreach (var item in element.Elements ())
                    arr.Add (ParseObject (item));
                return arr;
            case "dict":
                var dict = new PlistDictionary ();
                var child = element.Elements ().FirstOrDefault ();
                while (child != null) {
                    var childName = child.Name.LocalName;
                    if (childName.ToLower () != "key")
                        throw new Exception ("expected <key> element inside <dict>, got <" + childName + ">");

                    var key = child.Value;

                    child = NextElement (child);

                    if (child == null)
                        throw new Exception ("expected a value element after <key> (" + key + ") inside <dict>");

                    dict.Add (key, ParseObject (child));

                    child = NextElement (child);
                }
                return dict;
            default:
                throw new Exception ("unexpected element <" + elementName + ">");
            }
        }

        static XElement NextElement (XElement element)
        {
            XNode node = element;
            while (node != null) {
                node = node.NextNode;
                element = node as XElement;
                if (element != null)
                    return element;
            }
            return null;
        }
    }

    #endregion
}