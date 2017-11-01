//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.Tests
{
    static class ClientSessionUriShouldExtensions
    {
        public static void ShouldEqual (this ClientSessionUri a, ClientSessionUri b)
        {
            ((object)a).ShouldEqual (b);
            ((object)new ClientSessionUri (new Uri (a.ToString ())))
                .ShouldEqual (new ClientSessionUri (new Uri (b.ToString ())));
            (a == b).ShouldBeTrue ();
            (a != b).ShouldBeFalse ();
        }

        public static void ShouldNotEqual (this ClientSessionUri a, ClientSessionUri b)
        {
            ((object)a).ShouldNotEqual (b);
            ((object)new ClientSessionUri (new Uri (a.ToString ())))
                .ShouldNotEqual (new ClientSessionUri (new Uri (b.ToString ())));
            (a == b).ShouldBeFalse ();
            (a != b).ShouldBeTrue ();
        }
    }

    [TestFixture]
    class ClientSessionUriTests
    {
        static ClientSessionUri CSU (string uriWithoutScheme)
            => new ClientSessionUri (new Uri ("xamarin-interactive://" + uriWithoutScheme));

        [Test]
        public void Schemes ()
        {
            Assert.Throws<ArgumentException> (
                () => new ClientSessionUri (new Uri ("http://foo")));
            Assert.Throws<ArgumentException> (
                () => new ClientSessionUri (new Uri ("relative path", UriKind.Relative)));

            Assert.DoesNotThrow (() => new ClientSessionUri (new Uri ("file://foo")));
            Assert.DoesNotThrow (() => new ClientSessionUri (new Uri ("C:\\file")));
            Assert.DoesNotThrow (() => CSU ("localhost:54321"));

            ClientSessionUri.IsSchemeSupported ("xamarin-interactive").ShouldBeTrue ();
            ClientSessionUri.IsSchemeSupported ("file").ShouldBeTrue ();
            ClientSessionUri.IsSchemeSupported ("gopher").ShouldBeFalse ();
        }

        [Test]
        public void WorkingDirectory ()
        {
            const string uriString = "bacon:60000/v1?workingDirectory=/a/b/c";

            var referenceUri = new ClientSessionUri (
                AgentType.Unknown,
                ClientSessionKind.Unknown,
                "bacon",
                60000,
                workingDirectory: "/a/b/c");

            CSU (uriString).ShouldEqual (referenceUri);

            CSU (uriString)
                .WithWorkingDirectory ("overwritten")
                .ShouldEqual (referenceUri.WithWorkingDirectory ("overwritten"));
        }

        [Test]
        public void QueryStringWithoutVersion ()
        {
            Assert.Throws<ArgumentException> (() => CSU ("/?qp"));
            Assert.DoesNotThrow (() => CSU ("/v1?qp"));
            Assert.DoesNotThrow (() => CSU ("/"));
        }

        [Test]
        public void VersionAndQueryString ()
        {
            new Uri (new ClientSessionUri ("localhost", 54321).ToString ())
                .AbsolutePath.ShouldEqual ("/");
            new Uri (new ClientSessionUri ("localhost", 54321, new [] { "path" }).ToString ())
                .AbsolutePath.ShouldEqual ("/v1");
        }

        [Test]
        public void HostPortAssemblyPaths ()
        {
            CSU ("bacon:60000").ShouldEqual (new ClientSessionUri ("bacon", 60000));
            CSU ("bacon:60000/").ShouldEqual (new ClientSessionUri ("bacon", 60000));

            CSU ("porkbelly:54321/v1?assemblySearchPath=/a/b/c&assemblySearchPath=w%26%3Dx")
                .ShouldEqual (new ClientSessionUri ("porkbelly", 54321, new [] {
                    "/a/b/c",
                    "w&=x"
                }));
        }

        [Test]
        public void NewWorkbook ()
        {
            CSU ("/v1?sessionKind=Workbook&agentType=MacNet45")
                .ShouldEqual (new ClientSessionUri (AgentType.MacNet45, ClientSessionKind.Workbook));
        }

        [TestCase ("file:///workbookpath")]
        [TestCase ("C:\\workbook path")]
        [TestCase ("C:\\workbookpath")]
        [TestCase ("file:///workbook%20path")]
        public void OpenWorkbook (string path)
        {
            var systemUri = new Uri (path);
            var uri = new ClientSessionUri (systemUri);
            uri.WorkbookPath.ShouldEqual (systemUri.LocalPath);
            uri.SessionKind.ShouldEqual (ClientSessionKind.Workbook);
            uri.ToString ().ShouldEqual (systemUri.ToString ());
        }

        [TestCase ("file:///workbookpath/")]
        [TestCase ("C:\\workbook\\path\\")]
        [TestCase ("C:\\workbook\\path\\\\\\//")]
        // Finder will provide URLs to directories (packages) with a trailing
        // '/' meanwhile the File->Open dialog on Mac will provide the same
        // URL without a trailing '/'. This manifested in Finder-opened packages
        // showing up without a title because the workbook title is derived
        // from the filename of the path by default (so "foo.workbook/" was
        // titled "" when opened via Finder and the same "foo.workbook" was
        // correctly titled "foo" when opened via File->Open. Thanks Finder!
        // CSU now trims trailing slashes.
        public void OpenWorkbookWithTrailingSlash (string path)
        {
            var systemUri = new Uri (path);
            new ClientSessionUri (systemUri)
                .WorkbookPath
                .ShouldEqual (systemUri.LocalPath.TrimEnd (new [] { '/', '\\' }));
        }

        [Test]
        public void WithAssemblySearchPaths ()
        {
            var original = new ClientSessionUri (AgentType.iOS, ClientSessionKind.LiveInspection);
            original.ShouldBeSameAs (original.WithAssemblySearchPaths (null));
            original.ShouldBeSameAs (original.WithAssemblySearchPaths (new string [0]));

            var modified = original.WithAssemblySearchPaths (new [] { "hello" });
            modified.AssemblySearchPaths.Length.ShouldEqual (1);
            modified.AssemblySearchPaths [0].ShouldEqual ("hello");

            original = new ClientSessionUri ("localhost", 54321, new [] { "a", "b", "c" });
            original.ShouldBeSameAs (original.WithAssemblySearchPaths (new [] { "a", "b", "c" }));
            original.WithAssemblySearchPaths (null).AssemblySearchPaths.Length.ShouldEqual (0);
            original.WithAssemblySearchPaths (new string [0]).AssemblySearchPaths.Length.ShouldEqual (0);

            modified = original.WithAssemblySearchPaths (new [] { "x", "y" });
            modified.AssemblySearchPaths.Length.ShouldEqual (2);
            modified.AssemblySearchPaths [0].ShouldEqual ("x");
            modified.AssemblySearchPaths [1].ShouldEqual ("y");
        }

        [Test]
        public void WithParameters ()
        {
            var original = new ClientSessionUri (AgentType.iOS, ClientSessionKind.Workbook);
            original.ShouldBeSameAs (original.WithParameters (null));
            original.ShouldBeSameAs (original.WithParameters (Array.Empty<KeyValuePair<string, string>> ()));

            var parameters = new [] {
                new KeyValuePair<string, string> ("feature", "xamarin.forms"),
                new KeyValuePair<string, string> ("random thing", "blah & blah")
            };

            var modified = original.WithParameters (parameters);
            modified.Parameters.Length.ShouldEqual (2);
            modified.Parameters.SequenceShouldEqual (parameters);

            var fromSystemUri = CSU ("/v1?sessionKind=Workbook&agentType=iOS&feature=xamarin.forms&random%20thing=blah%20%26%20blah");
            fromSystemUri.ShouldEqual (modified);
        }

        [Test]
        public void WithHostAndPort ()
        {
            var original = new ClientSessionUri ("localhost", 54321);
            original.ShouldBeSameAs (original.WithHostAndPort (null, null));
            original.ShouldBeSameAs (original.WithHostAndPort ("localhost", null));
            original.ShouldBeSameAs (original.WithHostAndPort (null, 54321));
            original.ShouldBeSameAs (original.WithHostAndPort ("localhost", 54321));

            var modifiedHost = original.WithHostAndPort ("127.0.0.1", null);
            original.ShouldNotEqual (modifiedHost);
            modifiedHost.Host.ShouldEqual ("127.0.0.1");
            modifiedHost.Port.ShouldEqual (original.Port);
            original.ShouldEqual (modifiedHost.WithHostAndPort (original.Host, null));

            var modifiedPort = original.WithHostAndPort (null, 50000);
            original.ShouldNotEqual (modifiedPort);
            modifiedPort.Host.ShouldEqual (original.Host);
            modifiedPort.Port.ShouldEqual ((ushort)50000);
            original.ShouldEqual (modifiedPort.WithHostAndPort (null, original.Port));


            var modifiedBoth = original.WithHostAndPort ("127.0.0.1", 50000);
            original.ShouldNotEqual (modifiedBoth);
            modifiedBoth.Host.ShouldEqual ("127.0.0.1");
            modifiedBoth.Port.ShouldEqual ((ushort)50000);
            original.ShouldEqual (modifiedBoth.WithHostAndPort (original.Host, original.Port));
        }

        [TestCase ("54321", true, "127.0.0.1", 54321, ClientSessionKind.Unknown)]
        [TestCase (":54321", true, "127.0.0.1", 54321, ClientSessionKind.Unknown)]
        [TestCase ("host", false, null, 0, ClientSessionKind.Unknown)]
        [TestCase ("host:54321", true, "host", 54321, ClientSessionKind.Unknown)]
        [TestCase ("127.0.0.1:54321", true, "127.0.0.1", 54321, ClientSessionKind.Unknown)]
        [TestCase ("100", false, null, 0, ClientSessionKind.Unknown)]
        [TestCase ("xamarin-interactive://54321", false, null, 0, ClientSessionKind.Unknown)]
        [TestCase ("xamarin-interactive://host:54321", true, "host", 54321, ClientSessionKind.Unknown)]
        [TestCase ("/v1?sessionKind=LiveInspection", true, null, 0, ClientSessionKind.LiveInspection)]
        [TestCase ("/v1?sessionKind=Workbook", true, null, 0, ClientSessionKind.Workbook)]
        [TestCase ("xamarin-interactive:///v1?sessionKind=LiveInspection", true, null, 0, ClientSessionKind.LiveInspection)]
        [TestCase ("xamarin-interactive:///v1?sessionKind=Workbook", true, null, 0, ClientSessionKind.Workbook)]
        [TestCase ("/foo/bar", true, null, 0, ClientSessionKind.Workbook)]
        [TestCase ("file:///foo/bar", true, null, 0, ClientSessionKind.Workbook)]
        [TestCase ("/", true, null, 0, ClientSessionKind.Workbook)]
        public void TryParse (
            string uriString,
            bool expectedResult,
            string host,
            int port,
            ClientSessionKind clientSessionKind)
        {
            ClientSessionUri uri;
            ClientSessionUri.TryParse (uriString, out uri).ShouldEqual (expectedResult);

            if (!expectedResult)
                return;

            uri.Host.ShouldEqual (host);
            uri.Port.ShouldEqual ((ushort)port);
            uri.SessionKind.ShouldEqual (clientSessionKind);

            Assert.DoesNotThrow (() => uri.ToString ());
        }

        #region ParseQueryString Tests

        delegate IReadOnlyList<KeyValuePair<string, string>> ParseQueryStringDelegate (string query);

        static readonly ParseQueryStringDelegate Parse
            = (ParseQueryStringDelegate)typeof (ClientSessionUri).GetMethod (
                "ParseQueryString",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
            .CreateDelegate (typeof (ParseQueryStringDelegate));

        [Test]
        public void Empty ()
        {
            Parse (null).Count.ShouldEqual (0);
            Parse (String.Empty).Count.ShouldEqual (0);
        }

        [Test]
        public void SingleKeyNoValue ()
        {
            var items = Parse ("singleKey");
            items.Count.ShouldEqual (1);
            items [0].Key.ShouldEqual ("singleKey");
            items [0].Value.ShouldBeNull ();

            items = Parse ("singleKey%26notAKey");
            items.Count.ShouldEqual (1);
            items [0].Key.ShouldEqual ("singleKey&notAKey");
            items [0].Value.ShouldBeNull ();

            items = Parse ("singleKey%3DnotAValue%26notAKey");
            items.Count.ShouldEqual (1);
            items [0].Key.ShouldEqual ("singleKey=notAValue&notAKey");
            items [0].Value.ShouldBeNull ();
        }

        [Test]
        public void MultipleKeysNoValue ()
        {
            var items = Parse ("key1&key2");
            items.Count.ShouldEqual (2);
            items [0].Key.ShouldEqual ("key1");
            items [0].Value.ShouldBeNull ();
            items [1].Key.ShouldEqual ("key2");
            items [1].Value.ShouldBeNull ();

            items = Parse ("key1&key2&key3%26notKey4");
            items.Count.ShouldEqual (3);
            items [0].Key.ShouldEqual ("key1");
            items [0].Value.ShouldBeNull ();
            items [1].Key.ShouldEqual ("key2");
            items [1].Value.ShouldBeNull ();
            items [2].Key.ShouldEqual ("key3&notKey4");
            items [2].Value.ShouldBeNull ();
        }

        [Test]
        public void EmptyValues ()
        {
            var items = Parse ("key1=&key2=&key3=notempty");
            items.Count.ShouldEqual (3);
            items [0].Key.ShouldEqual ("key1");
            items [0].Value.ShouldBeNull ();
            items [1].Key.ShouldEqual ("key2");
            items [1].Value.ShouldBeNull ();
            items [2].Key.ShouldEqual ("key3");
            items [2].Value.ShouldEqual ("notempty");
        }

        [Test]
        public void SingleKeyValue ()
        {
            var items = Parse ("key1=%3Dvalue1%26");
            items.Count.ShouldEqual (1);
            items [0].Key.ShouldEqual ("key1");
            items [0].Value.ShouldEqual ("=value1&");
        }

        [Test]
        public void MultipleKeysAndValues ()
        {
            var items = Parse ("key1=value1&key2=value2&key3=value3");
            items.Count.ShouldEqual (3);
            items [0].Key.ShouldEqual ("key1");
            items [0].Value.ShouldEqual ("value1");
            items [1].Key.ShouldEqual ("key2");
            items [1].Value.ShouldEqual ("value2");
            items [2].Key.ShouldEqual ("key3");
            items [2].Value.ShouldEqual ("value3");
        }

        #endregion
    }
}