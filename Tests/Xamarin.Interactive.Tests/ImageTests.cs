//
// ImageTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public sealed class ImageTests
    {
        [Test]
        public void Sniff_Png ()
        {
            Image.FromData (new byte [] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Png);

            Image.FromData (new byte [] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Png);
        }

        [Test]
        public void Sniff_Jpeg ()
        {
            Image.FromData (new byte [] {
                0xFF, 0xD8, 0xFF, 0xDB, 0xAA, 0xBB, 0xCC, 0xDD
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Jpeg);

            Image.FromData (new byte [] {
                0xFF, 0xD8, 0xFF, 0xE0, 0x11, 0x22, 0x4A, 0x46,
                0x49, 0x46, 0x00, 0x01
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Jpeg);

            Image.FromData (new byte [] {
                0xFF, 0xD8, 0xFF, 0xE1, 0x11, 0x22, 0x45, 0x78,
                0x69, 0x66, 0x00, 0x00
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Jpeg);
        }

        [Test]
        public void Sniff_Gif ()
        {
            Image.FromData (new byte [] {
                0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0xAA, 0xBB
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Gif);

            Image.FromData (new byte [] {
                0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0xAA, 0xBB
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Gif);
        }

        [Test]
        public void Sniff_Svg ()
        {
            Image.FromData (new byte [] {
                (byte)'<', (byte)'s', (byte)'v', (byte)'g', 0, 0, 0, 0
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, (byte)'<', (byte)'s', (byte)'v', (byte)'g', 0, 0, 0
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, 0, (byte)'<', (byte)'s', (byte)'v', (byte)'g', 0, 0
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, 0, 0, (byte)'<', (byte)'s', (byte)'v', (byte)'g', 0
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, 0, 0, 0, (byte)'<', (byte)'s', (byte)'v', (byte)'g'
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, 0, 0, 0, 0, (byte)'<', (byte)'s', (byte)'v', (byte)'g'
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, 0, 0, 0, 0, 0, (byte)'<', (byte)'s', (byte)'v', (byte)'g'
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, 0, 0, 0, 0, 0, 0, (byte)'<', (byte)'s', (byte)'v', (byte)'g'
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, 0, 0, 0, 0, 0, 0, 0, (byte)'<', (byte)'s', (byte)'v', (byte)'g'
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (new byte [] {
                0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)'<', (byte)'s', (byte)'v', (byte)'g'
            }).ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);

            Image.FromData (@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""no""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">
<svg width=""300"" height=""250"" version=""1.1"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xmlns=""http://www.w3.org/2000/svg"">")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Svg);
        }

        [Test]
        public void Sniff_Uri ()
        {
            Image.FromData ("data:image/svg+xml;utf8,<svg>")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("http://foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("https://foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("file://foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("/foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("../foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("./foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("C:\\foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("\\foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);

            Image.FromData ("\\\\foo")
                .ShouldNotBeNull ().Format.ShouldEqual (ImageFormat.Uri);
        }
    }
}