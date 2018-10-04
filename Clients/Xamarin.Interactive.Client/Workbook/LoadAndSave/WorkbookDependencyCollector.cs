//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.LoadAndSave
{
    sealed class WorkbookDependencyCollector
    {
        public Dictionary<WorkbookPage, List<FilePath>> Visit (WorkbookPackage package)
        {
            if (package == null)
                throw new ArgumentNullException (nameof (package));

            var dependencies = new Dictionary<WorkbookPage, List<FilePath>> ();

            foreach (var page in package.Pages)
                dependencies.Add (page, Visit (page)
                    .Where (path => package.WorkingBasePath.Combine (path).FileExists)
                    .ToList ());

            return dependencies;
        }

        public IEnumerable<FilePath> Visit (WorkbookPage page)
        {
            if (page == null)
                throw new ArgumentNullException (nameof (page));

            foreach (var cell in page.Contents) {
                switch (cell) {
                case MarkdownCell markdownCell:
                    foreach (var dependency in Visit (markdownCell))
                        yield return dependency;
                    break;
                case CodeCell codeCell:
                    foreach (var dependency in Visit (codeCell))
                        yield return dependency;
                    break;
                }
            }
        }

        FilePath GetRelativePath (string path)
        {
            if (String.IsNullOrEmpty (path))
                return FilePath.Empty;

            try {
                new Uri (path, UriKind.Relative);
            } catch {
                return FilePath.Empty;
            }

            var filePath = new FilePath (path);
            if (!filePath.IsRooted)
                return filePath;

            return FilePath.Empty;
        }

        IEnumerable<FilePath> Visit (MarkdownCell markdownCell)
            => markdownCell
                .ToMarkdownDocumentBlock ()
                .AsEnumerable ()
                .Where (e => e.IsOpening && e.Inline?.TargetUrl != null)
                .Select (e => GetRelativePath (e.Inline.TargetUrl))
                .Where (path => !path.IsNull);

        IEnumerable<FilePath> Visit (CodeCell codeCell)
            => CSharpSyntaxTree
                .ParseText (
                    codeCell.Buffer.Value,
                    CSharpParseOptions.Default
                        .WithKind (SourceCodeKind.Script)
                        .WithLanguageVersion (LanguageVersion.CSharp7_3))
                .GetRoot ()
                .DescendantTrivia ()
                .Where (trivia => trivia.HasStructure && (
                    trivia.IsKind (SyntaxKind.LoadDirectiveTrivia) ||
                    trivia.IsKind (SyntaxKind.ReferenceDirectiveTrivia)))
                .Select (trivia => trivia.GetStructure ())
                .SelectMany (node => node.ChildTokens ())
                .Where (token => token.IsKind (SyntaxKind.StringLiteralToken))
                .Select (token => GetRelativePath (token.ValueText))
                .Where (path => !path.IsNull);
    }
}