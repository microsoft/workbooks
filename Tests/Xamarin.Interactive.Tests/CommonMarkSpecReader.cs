//
// CommonMarkSpecReader.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Tests
{
    public sealed class CommonMarkSpecReader
    {
        const string gitTag = "0.27";
        const string specUri = "https://raw.githubusercontent.com/jgm/CommonMark/" + gitTag + "/spec.txt";
        const int expectedExamplesInSpec = 622;

        public static async Task<StreamReader> FromGitSpecAsync ()
        {
            var outputPath = Path.Combine (
                Path.GetTempPath (),
                "CommonMarkSpecs");

            Directory.CreateDirectory (outputPath);

            outputPath = Path.Combine (outputPath, "spec-" + gitTag + ".commonmark");

            if (!File.Exists (outputPath)) {
                try {
                    using (var outputStream = File.Create (outputPath)) {
                        var request = WebRequest.CreateHttp (specUri);
                        var response = await request.GetResponseAsync ();
                        using (var inputStream = response.GetResponseStream ())
                            await inputStream.CopyToAsync (outputStream);
                    }
                } catch {
                    File.Delete (outputPath);
                    throw;
                }
            }

            return new StreamReader (outputPath);
        }

        public static async Task<CommonMarkSpecReader> ExamplesFromGitSpecAsync ()
            => new CommonMarkSpecReader (await FromGitSpecAsync ());

        public static StreamReader FromGitSpec ()
        {
            StreamReader reader = null;
            var wait = new ManualResetEvent (false);
            ThreadPool.QueueUserWorkItem (o => {
                reader = FromGitSpecAsync ().Result;
                wait.Set ();
            });
            wait.WaitOne ();
            return reader;
        }

        public static CommonMarkSpecReader ExamplesFromGitSpec ()
            => new CommonMarkSpecReader (FromGitSpec ());

        readonly TextReader reader;

        public CommonMarkSpecReader (TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException (nameof (reader));

            this.reader = reader;
        }

        enum State
        {
            Documentation,
            ExampleSourceMarkdown,
            ExampleResultingHtml
        }

        const string startExampleDelimiter = "```````````````````````````````` example";
        const string endExampleDelimeter = "````````````````````````````````";
        const string exampleTransitionDelimeter = ".";

        public sealed class Example
        {
            public string Title { get; set; }
            public string ReferenceUrl { get; set; }
            public string CommonMark { get; set; }
            public string Html { get; set; }
        }

        public List<Example> Parse ()
        {
            var examples = new List<Example> (expectedExamplesInSpec);
            var exampleNumber = 0;
            Example example = null;

            var state = State.Documentation;
            var builder = new StringBuilder ();

            string line;
            while ((line = reader.ReadLine ()) != null) {
                line = line.Replace ('→', '\t');

                switch (state) {
                case State.Documentation:
                    if (line == startExampleDelimiter) {
                        exampleNumber++;
                        var title = $"example-{exampleNumber}";
                        example = new Example {
                            Title = title,
                            ReferenceUrl = $"http://spec.commonmark.org/{gitTag}/#{title}"
                        };
                        state = State.ExampleSourceMarkdown;
                    }
                    break;
                case State.ExampleSourceMarkdown:
                    if (line == exampleTransitionDelimeter) {
                        example.CommonMark = builder.ToString ();
                        builder.Clear ();
                        state = State.ExampleResultingHtml;
                    } else {
                        builder.AppendLine (line);
                    }
                    break;
                case State.ExampleResultingHtml:
                    if (line == endExampleDelimeter) {
                        example.Html = builder.ToString ();
                        builder.Clear ();
                        state = State.Documentation;

                        if (string.IsNullOrEmpty (example.CommonMark))
                            throw new Exception (
                                $"example {example.Title} has no CommonMark text");

                        examples.Add (example);
                    } else {
                        builder.AppendLine (line);
                    }
                    break;
                }
            }

            if (examples.Count != expectedExamplesInSpec)
                throw new Exception (
                    $"error parsing {gitTag} spec. expected {expectedExamplesInSpec} " +
                    $"examples but parsed only {examples.Count}.");

            return examples;
        }
    }
}