using System.Drawing;
using System.Linq;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    class ReflectionRepresentationProviderTests
    {
        const string prefix = "Xamarin.Interactive.Tests.Windows.";
        static readonly FilePath ResourcesRootPath = TestHelpers.PathToRepoRoot.Combine (
            "Tests",
            "Xamarin.Interactive.Tests",
            "Resources"
        );

        // Streams and files are tested separately because I saw some suggestions on various
        // SO posts/etc. that I saw while I was researching the error that precipitated
        // these tests being written (a NRE for an encoder inside S.D) that there may be a
        // difference in how the images are loaded and internally represented in those cases.

        [Test]
        [TestCase ("Resources.Xamburger.jpg", ImageFormat.Jpeg, 3264, 2448)]
        [TestCase ("Resources.Xamburger.png", ImageFormat.Png, 2448, 3264)]
        [TestCase ("Resources.Xamburger.gif", ImageFormat.Gif, 3264, 2448)]
        // TIFF is not a supported ImageFormat, so it should get transformed to PNG.
        [TestCase ("Resources.Xamburger.tif", ImageFormat.Png, 2448, 3264)]
        public void CanGetXIRImageFromStream (string imageName, ImageFormat expectedFormat, int width, int height)
        {
            var image = TestHelpers.GetResource<ReflectionRepresentationProviderTests> (prefix + imageName);
            var sdBitmap = new Bitmap (image);
            var repProvider = new ReflectionRepresentationProvider ();

            var reps = repProvider.ProvideRepresentations (sdBitmap);

            reps.Count ().ShouldEqual (1);

            var firstRep = reps.First ();

            var xirImage = firstRep.ShouldBeType<Representations.Image> ();
            xirImage.Format.ShouldEqual (expectedFormat);
            xirImage.Width.ShouldEqual (width);
            xirImage.Height.ShouldEqual (height);
        }

        [Test]
        [TestCase ("Xamburger.jpg", ImageFormat.Jpeg, 3264, 2448)]
        [TestCase ("Xamburger.png", ImageFormat.Png, 2448, 3264)]
        [TestCase ("Xamburger.gif", ImageFormat.Gif, 3264, 2448)]
        // TIFF is not a supported ImageFormat, so it should get transformed to PNG.
        [TestCase ("Xamburger.tif", ImageFormat.Png, 2448, 3264)]
        public void CanGetXIRImageFromFile (string imageName, ImageFormat expectedFormat, int width, int height)
        {
            var sdBitmap = new Bitmap (ResourcesRootPath.Combine (imageName));
            var repProvider = new ReflectionRepresentationProvider ();

            var reps = repProvider.ProvideRepresentations (sdBitmap);

            reps.Count ().ShouldEqual (1);

            var firstRep = reps.First ();

            var xirImage = firstRep.ShouldBeType<Representations.Image> ();
            xirImage.Format.ShouldEqual (expectedFormat);
            xirImage.Width.ShouldEqual (width);
            xirImage.Height.ShouldEqual (height);
        }
    }
}
