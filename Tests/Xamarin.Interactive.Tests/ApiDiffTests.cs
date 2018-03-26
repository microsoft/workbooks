//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

using NUnit.Framework;

namespace Xamarin.Interactive.Tests
{
    [TestFixture]
    public class ApiTests
    {
        [TestCase ("Agents/Xamarin.Interactive")]
        [TestCase ("CodeAnalysis/Xamarin.Interactive.CodeAnalysis")]
        // [TestCase ("Clients/Xamarin.Interactive.Client")]
        public void EnsureNoApiDifferenceAndLint (string basePath)
        {
            basePath = Path.Combine (TestHelpers.PathToRepoRoot, basePath);
            var assemblyName = Path.GetFileName (basePath);
            var assemblyFileName = Path.Combine (
                basePath,
                "bin",
                TestHelpers.Configuration,
                "netstandard2.0",
                assemblyName + ".dll");
            var apiReference = Path.Combine (
                TestHelpers.PathToRepoRoot,
                "docs",
                assemblyName + ".api.cs");

            var writer = new StringWriter ();
            var linter = new ApiDump.LintTool ();
            var driver = new ApiDump.Driver (assemblyFileName);
            driver.AddVisitorTool (linter);
            driver.Write (writer);

            writer.ToString ().ShouldEqual (
                File.ReadAllText (apiReference),
                ShouldEqualOptions.LineDiff);

            if (linter.Issues.Count > 0)
                Assert.Fail (string.Join (
                    "\n\n",
                    linter.Issues.Select (issue => issue.Description)));
        }
    }
}