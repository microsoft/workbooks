//
// MarkdownFormatterTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using CommonMark;
using CommonMark.Syntax;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

using Should;

namespace Xamarin.Interactive.Tests
{
	/// <summary>
	/// These tests ensure both that CommonMark.NET itself produces HTML output that is semantically
	/// identical to the CommonMark test suite cases, and that MarkdownFormatter also produces
	/// semantically identical markdown.
	/// 
	/// There are also separate tests for ensuring MarkdownFormatter does its best to both preserve
	/// syntax/style and output nice human readable syntax when it cannot perfectly preserve it.
	/// There are a number of shortcomings in CommonMark.NET (and even the reference implementations)
	/// that do not allow for perfect syntactic round-tripping of Markdown - important syntax details
	/// are not persisted on the AST.
	/// </summary>
	[TestFixture]
	public sealed class MarkdownFormatterTests
	{
		static CommonMarkSettings GetSettings (
			CommonMarkAdditionalFeatures additionalFeatures = CommonMarkAdditionalFeatures.None)
		{
			var settings = CommonMarkSettings
				.Default
				.WithMarkdownFormatter ();
			settings.AdditionalFeatures = additionalFeatures;

			// FIXME: spec cases 19, 40, and 56 can only pass when we track source
			// positions because significant white space is dropped by CommonMark.NET
			// parser. Recompute that significant space via source position tracking.
			settings.TrackSourcePosition = true;

			return settings;
		}

		static string NormalizeLines (string value)
			=> value.Replace ("\r\n", "\n").TrimEnd ('\n');

		// CommonMark.NET always has an extra trailing \n at the end of its HTML, and
		// in two test cases (31 and 140 in v0.27) places <li> contents immediately 
		// following the opening of the tag, whereas the specification places a \n.
		// between. These differences are acceptable, so scrub them away.
		static string NormalizeHtml (string html)
			=> NormalizeLines (html).Replace ("<li>\n", "<li>");

		static string PrefixLines (string prefix, string value)
		{
			if (value == null)
				return string.Empty;

			var builder = new StringBuilder ();
			foreach (var line in value.Split (new [] { '\n' }))
				builder.Append (prefix).AppendLine (line.Trim ('\r'));

			return builder.ToString ();
		}

		void AppendMarkdownDebug (StringBuilder message, string label, string markdown)
		{
			message.Append ("  ")
			       .Append (label)
			       .Append (":   ")
			       .Append (PrefixLines (
				       "            MD >| ",
				       markdown).TrimStart ());

			var debugSettings = GetSettings ().Clone ();
			debugSettings.OutputDelegate = null;
			debugSettings.OutputFormat = OutputFormat.SyntaxTree;

			message.Append (PrefixLines (
				"           AST >| ",
				CommonMarkConverter.Convert (markdown, debugSettings).TrimEnd ()));

			message.AppendLine ();
		}

		[Test, TestCaseSource (nameof (GetCommonMarkSpecTestCases))]
		public void CommonMarkSpecTest (CommonMarkSpecReader.Example example)
		{
			var message = new StringBuilder ();
			message.AppendLine ().AppendLine ();

			if (!string.IsNullOrEmpty (example.ReferenceUrl))
				message.Append ("    Spec: ").AppendLine (example.ReferenceUrl).AppendLine ();

			AppendMarkdownDebug (message, " Input", example.CommonMark);

			var referenceHtml = NormalizeHtml (example.Html);

			NormalizeHtml (CommonMarkConverter.Convert (example.CommonMark))
				.ShouldEqual (referenceHtml, message.ToString ());

			var newMarkdown = CommonMarkConverter.Convert (example.CommonMark, GetSettings ());
			AppendMarkdownDebug (message, "Output", newMarkdown);
			NormalizeHtml (CommonMarkConverter.Convert (newMarkdown))
				.ShouldEqual (referenceHtml, message.ToString ());
		}

		[Test, TestCaseSource (nameof (GetFormattingMdTestCases))]
		public void FormattingMdTest (string markdown)
			=> NormalizeLines (CommonMarkConverter.Convert (
				markdown,
				GetSettings (CommonMarkAdditionalFeatures.All)))
				.ShouldEqual (NormalizeLines (markdown));

		public static IEnumerable<ITestCaseData> GetCommonMarkSpecTestCases ()
		{
			var testCases = new List<ITestCaseData> ();
			foreach (var example in CommonMarkSpecReader.ExamplesFromGitSpec ().Parse ())
				testCases.Add (new TestCaseData (example) {
					TestName = "commonmark-spec-" + example.Title
				});
			return testCases;
		}

		public static IEnumerable<ITestCaseData> GetFormattingMdTestCases ()
		{
			const string match = "# Test Case: ";

			var cases = new List<TestCaseData> ();
			string caseName = null;
			var block = new StringBuilder ();

			Action yield = () => {
				if (caseName != null && block.Length > 0) {
					var runState = RunState.Runnable;

					var newCaseName = caseName.Replace ("*[skip]*", string.Empty);
					if (caseName.Length != newCaseName.Length)
						runState = RunState.Skipped;

					cases.Add (new TestCaseData (block.ToString ()) {
						TestName = newCaseName.Trim (),
						RunState = runState
					});

					block.Clear ();
				}
			};

			using (var streamReader = new StreamReader (TestHelpers.GetResource<MarkdownFormatterTests> (
				"Formatting.md"))) {
				string line;
				while ((line = streamReader.ReadLine ()) != null) {
					if (line.StartsWith (match, StringComparison.OrdinalIgnoreCase)) {
						yield ();
						caseName = line.Substring (match.Length).Trim ();
					} else {
						if (block.Length > 0)
							block.AppendLine ();
						block.Append (line);
					}
				}
			}

			yield ();
			return cases;
		}

		// The name of this class cracks me up. ❤️ Boston.
		class TeePass
		{
			public string Markdown { get; }
			public Block Block { get; }
			public string Html { get; }

			public TimeSpan ParseTime { get; }
			public TimeSpan FormatTime { get; }

			public TeePass (string markdown)
			{
				var stopwatch = new Stopwatch ();
				stopwatch.Start ();
				Block = CommonMarkConverter.Parse (markdown);
				ParseTime = stopwatch.Elapsed;

				using (var writer = new StringWriter ()) {
					stopwatch.Restart ();
					CommonMarkConverter.ProcessStage3 (Block, writer);
					FormatTime = stopwatch.Elapsed;
					Html = writer.ToString ();
				}
			}

			public TeePass (Block block)
			{
				var stopwatch = new Stopwatch ();
				stopwatch.Start ();
				Markdown = block.ToMarkdownString ();
				FormatTime = stopwatch.Elapsed;
				Html = CommonMarkConverter.Convert (Markdown);
			}
		}

		[Test]
		public void SemanticRoundTripFullSpec ()
		{
			const int passes = 100;

			var markdownParseDuration = TimeSpan.Zero;
			var htmlFormatDuration = TimeSpan.Zero;
			var markdownFormatDuration = TimeSpan.Zero;

			for (int i = 0; i < passes; i++) {
				TeePass referencePass;
				using (var reader = CommonMarkSpecReader.FromGitSpec ())
					referencePass = new TeePass (reader.ReadToEnd ());

				var roundtripPass = new TeePass (referencePass.Block);
				roundtripPass.Html.ShouldEqual (referencePass.Html);

				markdownParseDuration += referencePass.ParseTime;
				htmlFormatDuration += referencePass.FormatTime;
				markdownFormatDuration += roundtripPass.FormatTime;
			}

			Action<string, TimeSpan> log = (label, duration) =>
				Console.WriteLine (
					"        {0}: {1}s total over {2} passes ({3}s average)",
					label,
					duration.TotalSeconds,
					passes,
					duration.TotalSeconds / passes);

			Console.WriteLine ();
			log (" Markdown Parse", markdownParseDuration);
			log ("    HTML Format", htmlFormatDuration);
			log ("Markdown Format", markdownFormatDuration);
			Console.WriteLine ();
		}
 	}
}