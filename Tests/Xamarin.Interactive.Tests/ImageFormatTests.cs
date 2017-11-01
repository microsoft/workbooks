//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    sealed class ImageFormatTests
    {
        static void Assert (Image image)
        {

#if MAC
			const string resourcePrefix = "Xamarin.Interactive.Tests.Mac.Resources.ImageFormatTests.";
			const string platformPrefix = "Mac-";
			var provider = new Interactive.Mac.MacRepresentationProvider ();
#elif WPF
			const string resourcePrefix = "Xamarin.Interactive.Tests.Windows.Resources.ImageFormatTests.";
			const string platformPrefix = "Wpf-";
			var provider = new Interactive.Wpf.WpfRepresentationProvider ();
#elif __ANDROID__
			const string resourcePrefix = "Xamarin.Interactive.Tests.Android.Resources.";
			const string platformPrefix = "Android-";
			var provider = new Interactive.Android.AndroidRepresentationProvider ();
#endif
            var resourceId = $"{resourcePrefix}{platformPrefix}{image.Format}.png";

            using (var referenceStream = new MemoryStream ()) {
                Assembly.GetExecutingAssembly ()
                    .GetManifestResourceStream (resourceId)
                    .CopyTo (referenceStream);

                var normalizedImage = (Image)provider.NormalizeRepresentation (image);
                NUnit.Framework.Assert.IsTrue (normalizedImage.Data.SequenceEqual (referenceStream.ToArray ()));
            }
        }

        static readonly byte [] Rgb24Buffer = {
            0xff, 0x00, 0x00,
            0x00, 0xff, 0x00,
            0x00, 0x00, 0xff
        };

        [Test]
        public void Rgb24 ()
            => Assert (new Image (ImageFormat.Rgb24, Rgb24Buffer, 1, 3));

        static readonly byte [] Rgba32Buffer = {
            0xff, 0x00, 0x00, 0x55,
            0x00, 0xff, 0x00, 0x55,
            0x00, 0x00, 0xff, 0x55
        };

        [Test]
        public void Rgba32 ()
            => Assert (new Image (ImageFormat.Rgba32, Rgba32Buffer, 1, 3));

        static readonly byte [] Bgr24Buffer = {
            0x00, 0x00, 0xff,
            0x00, 0xff, 0x00,
            0xff, 0x00, 0x00
        };

        [Test]
        public void Bgr24 ()
            => Assert (new Image (ImageFormat.Bgr24, Bgr24Buffer, 1, 3));

        static readonly byte [] Bgra32Buffer = {
            0x00, 0x00, 0xff, 0x55,
            0x00, 0xff, 0x00, 0x55,
            0xff, 0x00, 0x00, 0x55
        };

        [Test]
        public void Bgra32 ()
            => Assert (new Image (ImageFormat.Bgra32, Bgra32Buffer, 1, 3));
    }
}