//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    public sealed class Image
    {
        public static Image FromSvg (string svgData, int width = 0, int height = 0)
            => new Image (
                ImageFormat.Svg,
                Utf8.GetBytes (svgData),
                width,
                height);

        public static Image FromUri (string uri, int width = 0, int height = 0)
            => new Image (
                ImageFormat.Uri,
                Utf8.GetBytes (uri),
                width,
                height);

        static readonly Regex uriRegex = new Regex (
            @"^(\.{0,2}[\\\/]|(https?|data|file):|[a-z]:\\)",
            RegexOptions.IgnoreCase);

        public static Image FromData (string data, int width = 0, int height = 0)
        {
            if (uriRegex.IsMatch (data))
                return FromUri (data, width, height);

            return FromData (Utf8.GetBytes (data), width, height);
        }

        public static unsafe Image FromData (byte [] data, int width = 0, int height = 0)
        {
            if (data == null)
                return null;

            if (data.Length < 8)
                return null;

            var format = ImageFormat.Unknown;

            const uint svgMagic = 0x67_76_73_3C; // '<svg'

            fixed (byte* dataPtr = &data [0]) {
                switch (*(uint*)dataPtr) {
                case 0x47_4E_50_89:
                    format = ImageFormat.Png;
                    break;
                case 0x38_46_49_47:
                    format = ImageFormat.Gif;
                    break;
                case 0xDB_FF_D8_FF:
                case 0xE0_FF_D8_FF:
                case 0xE1_FF_D8_FF:
                    format = ImageFormat.Jpeg;
                    break;
                case svgMagic:
                    format = ImageFormat.Svg;
                    break;
                default:
                    for (int i = 1, n = Math.Min (data.Length, 512); i <= n - 4; i++) {
                        if (*(uint*)(dataPtr + i) == svgMagic) {
                            format = ImageFormat.Svg;
                            break;
                        }
                    }
                    break;
                }
            }

            return new Image (format, data, width, height);
        }

        public static async Task<Image> FromStreamAsync (
            Stream stream,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            if (stream == null)
                return null;

            using (var memoryStream = new MemoryStream ()) {
                await stream
                    .CopyToAsync (memoryStream, 4096, cancellationToken)
                    .ConfigureAwait (false);
                return FromData (memoryStream.ToArray ());
            }
        }

        [JsonConstructor]
        public Image (ImageFormat format, byte [] data, int width = 0, int height = 0, double scale = 1)
        {
            if (data == null)
                throw new ArgumentNullException (nameof (data));

            if (scale <= 0)
                throw new ArgumentOutOfRangeException (nameof (scale), "must be > 0");

            Format = format;
            Data = data;
            Width = width;
            Height = height;
            Scale = scale;
        }

        public ImageFormat Format { get; }
        public byte [] Data { get; }
        public int Width { get; }
        public int Height { get; }
        public double Scale { get; }
    }
}