//
// ApiDiffTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;

using NUnit.Framework;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class ApiTests
	{
		[TestCase (
			"Agents/Xamarin.Interactive",
			"Xamarin.Interactive.dll",
			"API/Xamarin.Interactive.api.cs")]
		public void EnsureNoApiDifferenceAndLint (
			string basePath,
			string assemblyFileName,
			string apiReference)
		{
			basePath = Path.Combine (TestHelpers.PathToRepoRoot, basePath);
			assemblyFileName = Path.Combine (
				basePath,
				"bin",
				TestHelpers.Configuration,
				assemblyFileName);
			apiReference = Path.Combine (basePath, apiReference);

			var writer = new StringWriter ();
			var linter = new ApiDump.LintTool ();
			var driver = new ApiDump.Driver (assemblyFileName);
			driver.AddVisitorTool (linter);
			driver.Write (writer);

			writer.ToString ().ShouldEqual (
				File.ReadAllText (apiReference),
				ShouldEqualOptions.LineDiff);

			foreach (var issue in linter.Issues)
				Assert.Fail (issue.Description);
		}
	}
}