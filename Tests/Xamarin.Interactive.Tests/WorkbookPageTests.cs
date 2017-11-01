//
// WorkbookPageTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;

using NUnit.Framework;

using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public sealed class WorkbookPageTests
	{
		const string emptyWorkbookGuid = "9222b263-b1eb-40f8-9942-bbbbbbbbbbbb";

		const string emptyWorkbookSource =
			"---\n" +
			"uti: com.xamarin.workbook\n" +
			"id: " + emptyWorkbookGuid + "\n" +
			"---";

		[TestCase (true)]
		[TestCase (false)]
		public void SaveEmptyWorkbook (bool readEmptyWorkbookSource)
		{
			var writer = new StringWriter ();
			var page = new WorkbookPage (new WorkbookPackage ());
			if (readEmptyWorkbookSource)
				page.Read (new StringReader (emptyWorkbookSource));
			else
				page.Manifest.Guid = Guid.Parse (emptyWorkbookGuid);
			page.Write (writer, null);
			writer.ToString ().ShouldEqual (
				emptyWorkbookSource,
				ShouldEqualOptions.LineDiff);
		}

		[Test]
		public void AppendCellToEmptyWorkbook ()
		{
			var page = new WorkbookPage (new WorkbookPackage ());
			page.Read (new StringReader (emptyWorkbookSource));
			page.Contents.AppendCell(new CodeCell ("csharp", "2 + 2"));
			var writer = new StringWriter ();
			page.Write (writer, null);
			writer.ToString ().ShouldEqual (
				emptyWorkbookSource + "\n\n```csharp\n2 + 2\n```",
				ShouldEqualOptions.LineDiff);
		}

		[Test]
		public void InsertNewHeadCellInWorkbookWithManifest ()
		{
			var page = new WorkbookPage (new WorkbookPackage ());
			page.Read (new StringReader (emptyWorkbookSource + "\n\n# Head Cell"));
			// page.Contents =>
			//   [0]: MarkdownWorkbookEntry
			//
			// Simulate the first save with no changes: the issue here was that
			// the write left the manifest in the model, so subsequent writes
			// would insert a new manifest and write the previous one.
			page.Write (new StringWriter (), null);
			// page.Contents =>
			//   [0]: YamlMetadataWorkbookEntry (!! this was the bug)
			//   [1]: MarkdownWorkbookEntry
			//
			// Now insert a new head cell:
			page.Contents.InsertCellBefore (
				page.Contents.FirstCell,
				new CodeCell ("csharp", "2 + 2"));
			// page.Contents =>
			//   [0]: CodeWorkbookEntry
			//   [1]: YamlMetadataWorkbookEntry
			//   [2]: MarkdownWorkbookEntry
			//
			// Now save again, and we'll get a new manifest entry at the head
			// since the previous one was left in the model and had a new cell
			// inserted before it.
			var writer = new StringWriter ();
			page.Write (writer, null);
			// page.Contents =>
			//   [0]: YamlMetadataWorkbookEntry
			//   [1]: CodeWorkbookEntry
			//   [2]: YamlMetadataWorkbookEntry
			//   [3]: MarkdownWorkbookEntry
			writer.ToString ().ShouldEqual (
				emptyWorkbookSource + "\n\n```csharp\n2 + 2\n```\n\n# Head Cell",
				ShouldEqualOptions.LineDiff);
		}
	}
}