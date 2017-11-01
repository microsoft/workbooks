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

using Should;

using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Tests
{
    static class WorkbookTestExtensions
    {
        public static string ToTrimmedString (this Cell entry)
        {
            return entry?.Buffer.Value?.Trim ();
        }
    }

    [TestFixture]
    public class WorkbookDocumentTests
    {
        static WorkbookDocument ParseText (string text)
        {
            var document = new WorkbookDocument ();
            document.Read (new StringReader (text));
            return document;
        }

        static void TestEntryLinkage (WorkbookDocument document,
            Cell newEntry,
            int expectedCount,
            Cell expectedFirstEntry,
            Cell expectedLastEntry,
            Cell expectedPreviousEntry,
            Cell expectedNextEntry)
        {
            newEntry.Document.ShouldEqual (document);
            document.Count.ShouldEqual (expectedCount);

            if (expectedFirstEntry == null)
                document.FirstCell.ShouldBeNull ();
            else
                document.FirstCell.ShouldEqual (expectedFirstEntry);

            if (expectedLastEntry == null)
                document.LastCell.ShouldBeNull ();
            else
                document.LastCell.ShouldEqual (expectedLastEntry);

            if (expectedPreviousEntry == null)
                newEntry.PreviousCell.ShouldBeNull ();
            else
                newEntry.PreviousCell.ShouldEqual (expectedPreviousEntry);

            if (expectedNextEntry == null)
                newEntry.NextCell.ShouldBeNull ();
            else
                newEntry.NextCell.ShouldEqual (expectedNextEntry);
        }

        [Test]
        public void Append ()
        {
            var doc = new WorkbookDocument ();
            doc.Count.ShouldEqual (0);

            var a = new MarkdownCell ();
            doc.AppendCell (a);
            TestEntryLinkage (doc, a, 1, a, a, null, null);

            var b = new MarkdownCell ();
            doc.AppendCell (b);
            TestEntryLinkage (doc, b, 2, a, b, a, null);

            var c = new MarkdownCell ();
            doc.AppendCell (c);
            TestEntryLinkage (doc, c, 3, a, c, b, null);

            var d = new MarkdownCell ();
            doc.AppendCell (d);
            TestEntryLinkage (doc, d, 4, a, d, c, null);

            TestEntryLinkage (doc, a, 4, a, d, null, b);
            TestEntryLinkage (doc, b, 4, a, d, a, c);
            TestEntryLinkage (doc, c, 4, a, d, b, d);
            TestEntryLinkage (doc, d, 4, a, d, c, null);
        }

        [Test]
        public void Remove ()
        {
            var doc = new WorkbookDocument ();

            var a = new MarkdownCell ();
            doc.AppendCell (a);
            doc.Count.ShouldEqual (1);
            doc.RemoveCell (a);
            doc.Count.ShouldEqual (0);
            a.NextCell.ShouldBeNull ();
            a.PreviousCell.ShouldBeNull ();
            doc.FirstCell.ShouldBeNull ();
            doc.LastCell.ShouldBeNull ();

            var b = new MarkdownCell ();
            doc.AppendCell (a);
            doc.AppendCell (b);
            doc.FirstCell.ShouldEqual (a);
            doc.LastCell.ShouldEqual (b);
            a.NextCell.ShouldEqual (b);
            a.PreviousCell.ShouldBeNull ();
            b.NextCell.ShouldBeNull ();
            b.PreviousCell.ShouldEqual (a);

            doc.RemoveCell (b);
            doc.Count.ShouldEqual (1);
            doc.FirstCell.ShouldEqual (a);
            doc.LastCell.ShouldEqual (a);
            b.NextCell.ShouldBeNull ();
            b.PreviousCell.ShouldBeNull ();
            a.NextCell.ShouldBeNull ();
            a.PreviousCell.ShouldBeNull ();

            doc.RemoveCell (a);

            var c = new MarkdownCell ();
            doc.AppendCell (a);
            doc.AppendCell (b);
            doc.AppendCell (c);
            doc.FirstCell.ShouldEqual (a);
            doc.LastCell.ShouldEqual (c);
            a.NextCell.ShouldEqual (b);
            a.PreviousCell.ShouldBeNull ();
            b.NextCell.ShouldEqual (c);
            b.PreviousCell.ShouldEqual (a);
            c.NextCell.ShouldBeNull ();
            c.PreviousCell.ShouldEqual (b);

            doc.RemoveCell (b);
            doc.FirstCell.ShouldEqual (a);
            doc.LastCell.ShouldEqual (c);
            a.NextCell.ShouldEqual (c);
            a.PreviousCell.ShouldBeNull ();
            b.NextCell.ShouldBeNull ();
            b.PreviousCell.ShouldBeNull ();
            c.NextCell.ShouldBeNull ();
            c.PreviousCell.ShouldEqual (a);
        }

        [Test]
        public void InsertAfter ()
        {
            var doc = new WorkbookDocument ();

            // [a]
            var a = new MarkdownCell ();
            doc.AppendCell (a);
            TestEntryLinkage (doc, a, 1, a, a, null, null);

            // [a, b]
            var b = new MarkdownCell ();
            doc.InsertCellAfter (a, b);
            TestEntryLinkage (doc, b, 2, a, b, a, null);

            // [a, c, b]
            var c = new MarkdownCell ();
            doc.InsertCellAfter (a, c);
            TestEntryLinkage (doc, c, 3, a, b, a, b);

            // [a, c, d, b]
            var d = new MarkdownCell ();
            doc.InsertCellAfter (c, d);
            TestEntryLinkage (doc, d, 4, a, b, c, b);

            // [a, c, d, b]
            TestEntryLinkage (doc, a, 4, a, b, null, c);
            TestEntryLinkage (doc, c, 4, a, b, a, d);
            TestEntryLinkage (doc, d, 4, a, b, c, b);
            TestEntryLinkage (doc, b, 4, a, b, d, null);
        }

        [Test]
        public void InsertBefore ()
        {
            var doc = new WorkbookDocument ();

            // [a]
            var a = new MarkdownCell ();
            doc.AppendCell (a);
            TestEntryLinkage (doc, a, 1, a, a, null, null);

            // [b, a]
            var b = new MarkdownCell ();
            doc.InsertCellBefore (a, b);
            TestEntryLinkage (doc, b, 2, b, a, null, a);

            // [b, c, a]
            var c = new MarkdownCell ();
            doc.InsertCellBefore (a, c);
            TestEntryLinkage (doc, c, 3, b, a, b, a);

            // [b, d, c, a]
            var d = new MarkdownCell ();
            doc.InsertCellBefore (c, d);
            TestEntryLinkage (doc, d, 4, b, a, b, c);

            // [b, d, c, a]
            TestEntryLinkage (doc, b, 4, b, a, null, d);
            TestEntryLinkage (doc, d, 4, b, a, b, c);
            TestEntryLinkage (doc, c, 4, b, a, d, a);
            TestEntryLinkage (doc, a, 4, b, a, c, null);
        }

        [Test]
        public void Enumerate ()
        {
            var doc = new WorkbookDocument ();
            doc.GetEnumerator ().MoveNext ().ShouldBeFalse ();

            doc.AppendCell (new CodeCell ("x", "a"));
            doc.AppendCell (new CodeCell ("x", "b"));
            doc.AppendCell (new CodeCell ("x", "c"));
            doc.AppendCell (new CodeCell ("x", "d"));

            doc.Select (e => e.Buffer.Value).ToArray ().ShouldEqual (new [] { "a", "b", "c", "d" });
        }

        [Test]
        public void Index ()
        {
            var doc = new WorkbookDocument ();

            doc.AppendCell (new CodeCell ("x", "a"));
            doc.AppendCell (new CodeCell ("x", "b"));
            doc.AppendCell (new CodeCell ("x", "c"));
            doc.AppendCell (new CodeCell ("x", "d"));

            doc [0].Buffer.Value.ShouldEqual ("a");
            doc [1].Buffer.Value.ShouldEqual ("b");
            doc [2].Buffer.Value.ShouldEqual ("c");
            doc [3].Buffer.Value.ShouldEqual ("d");

            Assert.Throws<IndexOutOfRangeException> (() => Assert.IsNotNull (doc [-1]));
            Assert.Throws<IndexOutOfRangeException> (() => Assert.IsNotNull (doc [4]));
        }

        [Test]
        public void EmptyDocument ()
        {
            new WorkbookDocument ().Count.ShouldEqual (0);
            ParseText ("").Count.ShouldEqual (0);
        }

        [Test]
        public void SingleCodeItemDocument ()
        {
            var document = ParseText (@"
```csharp
1 + 1
```");
            document.Count.ShouldEqual (1);
            document [0].ShouldBeInstanceOf<CodeCell> ();
            ((CodeCell)document [0]).LanguageName.ShouldEqual ("csharp");
            document [0].ToTrimmedString ().ShouldEqual ("1 + 1");
        }

        [Test]
        public void MixedDocument ()
        {
            var document = ParseText (@"
# notebook header

intro paragraph

```csharp
var pi = Math.PI
```

```csharp
pi * 2
```

another paragraph

```csharp
99
+
100
```

> goodbye
");
            document.Count.ShouldEqual (6);

            document [0].ShouldBeInstanceOf<MarkdownCell> ();
            document [0].ToTrimmedString ().ShouldEqual ($"# notebook header{Environment.NewLine}{Environment.NewLine}intro paragraph");

            document [1].ShouldBeInstanceOf<CodeCell> ();
            document [1].ToTrimmedString ().ShouldEqual ("var pi = Math.PI");

            document [2].ShouldBeInstanceOf<CodeCell> ();
            document [2].ToTrimmedString ().ShouldEqual ("pi * 2");

            document [3].ShouldBeInstanceOf<MarkdownCell> ();
            document [3].ToTrimmedString ().ShouldEqual ("another paragraph");

            document [4].ShouldBeInstanceOf<CodeCell> ();
            document [4].ToTrimmedString ().ShouldEqual ($"99\n+\n100");

            document [5].ShouldBeInstanceOf<MarkdownCell> ();
            document [5].ToTrimmedString ().ShouldEqual ("> goodbye");
        }

        [Test]
        public void CodeBlockExtraInfo ()
        {
            const string buffer = @"```csharp
1
```

```csharp     a    b   c
2
```";

            var document = ParseText (buffer);

            document.Count.ShouldEqual (2);

            document [0].ShouldBeInstanceOf<CodeCell> ().ExtraInfo.ShouldBeNull ();
            document [1].ShouldBeInstanceOf<CodeCell> ().ExtraInfo.ShouldEqual ("    a    b   c");

            var writer = new StringWriter ();
            document.Write (writer);
            writer.ToString ().ShouldEqual (buffer, ShouldEqualOptions.IgnoreLineEndings);
        }
    }
}