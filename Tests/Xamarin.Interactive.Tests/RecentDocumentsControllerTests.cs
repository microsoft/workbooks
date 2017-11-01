//
// RecentDocumentsControllerTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.IO;
using System.Linq;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.IO;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    sealed class RecentDocumentsControllerTests
    {
        const string yaml = @"- path: a
- path: b
- path: c
  title: Title of C
- path: d
";
        static readonly FilePath path = ClientApp
            .SharedInstance
            .FileSystem
            .GetTempDirectory ("tests")
            .Combine ("recent.yaml");

        [SetUp]
        public void SetUp ()
            => File.Delete (path);

        [Test]
        public void Load ()
        {
            File.WriteAllText (path, yaml);

            var c = new RecentDocumentsController (path, d => true);
            c.SequenceShouldEqual (new [] {
                new RecentDocument ("a"),
                new RecentDocument ("b"),
                new RecentDocument ("c"),
                new RecentDocument ("d")
            });
        }

        [Test]
        public void Save ()
        {
            new RecentDocumentsController (path, d => true) {
                new RecentDocument ("d"),
                new RecentDocument ("c", "Title of C"),
                new RecentDocument ("b"),
                new RecentDocument ("a")
            };

            File.ReadAllText (path).ShouldEqual (yaml, ShouldEqualOptions.IgnoreLineEndings);
        }
    }
}