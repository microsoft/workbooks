//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using NUnit.Framework;

using Xamarin.Interactive.Collections;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public class ObservableCollectionTests
    {
        [TestCase (null, null)]
        [TestCase (null, "one")]
        [TestCase ("one", null)]
        [TestCase ("one", "one")]
        [TestCase ("one", "one,one")]
        [TestCase ("one,two", ",one,two,two")]
        [TestCase ("one,two", "one")]
        [TestCase ("a,b", "a,b,a,b,a,b,a")]
        public void UpdateTo (string fromItemsStr, string toItemsStr)
        {
            var fromItems = fromItemsStr?.Split (',') ?? Array.Empty<string> ();
            var toItems = toItemsStr?.Split (',') ?? Array.Empty<string> ();

            var collection = new ObservableCollection<string> ();

            collection.UpdateTo (fromItems);
            collection.SequenceShouldEqual (fromItems);

            collection.UpdateTo (toItems);
            collection.SequenceShouldEqual (toItems);
        }
    }
}