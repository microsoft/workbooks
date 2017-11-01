//
// IdentifyAgentRequestTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public class IdentifyAgentRequestTests
    {
        [TestCase (0)]
        [TestCase (1)]
        [TestCase (2)]
        [TestCase (3)]
        [TestCase (4)]
        [TestCase (5)]
        public void CommandLineRoundTrip (int prependedArgumentCount)
        {
            var original = IdentifyAgentRequest.CreateWithBaseConnectUri (
                new Uri ("http://catoverflow.com"));

            var arguments = new List<string> ();
            for (int i = 0; i < prependedArgumentCount; i++)
                arguments.Add (Guid.NewGuid ().ToString ());

            arguments.AddRange (original.ToCommandLineArguments ());

            IdentifyAgentRequest
                .FromCommandLineArguments (arguments.ToArray ())
                .ShouldEqual (original);
        }
    }
}