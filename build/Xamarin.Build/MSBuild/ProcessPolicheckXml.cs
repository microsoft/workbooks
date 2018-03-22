//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public class ProcessPolicheckXml : Task
    {
        // A set of terms that should be excluded from the report.
        static readonly HashSet<int> ExcludedTerms = new HashSet<int> {
            80137,  // "race"
            209526, // "metro"
            211972, // "edge"
        };

        [Required]
        public string PolicheckXml { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public bool FailOnNonZeroTerms { get; set; } = true;

        public override bool Execute ()
        {
            if (!File.Exists (PolicheckXml)) {
                Log.LogError ($"Source XML file {PolicheckXml} does not exist.");
                return false;
            }

            var polidoc = XDocument.Load (PolicheckXml);
            var terms = new List<PolicheckTerm> ();
            var termElements = polidoc.Root.Element ("TermTbl").Elements ("Term");

            foreach (var termElement in termElements) {
                var termId = int.Parse (termElement.Attribute ("TermId")?.Value ?? "0");

                if (ExcludedTerms.Contains (termId))
                    continue;

                var actionElements = termElement.Element ("Actions").Elements ().ToArray ();
                var actions = new List<PolicheckAction> ();

                for (var i = 0; i < actionElements.Count (); i += 2) {
                    var termAction = new PolicheckAction {
                        Context = actionElements[i].Value,
                        Recommendation = actionElements[i+1].Value
                    };
                    actions.Add (termAction);
                }

                var term = new PolicheckTerm {
                    Actions = actions.ToArray (),
                    CaseSensitive = termElement.Attribute ("CaseSensitive")?.Value,
                    Comment = termElement.Element ("Comment")?.Value,
                    CommentSearch = termElement.Attribute ("CommentSearch")?.Value,
                    FieldsImpacted = termElement.Attribute ("FieldsImpacted")?.Value.Split (','),
                    RowCount = int.Parse (termElement.Attribute ("RowCount")?.Value ?? "0"),
                    Severity = int.Parse (termElement.Attribute ("Severity")?.Value ?? "0"),
                    TermClass = termElement.Attribute ("TermClass")?.Value,
                    TermDefinition = termElement.Attribute ("Term")?.Value,
                    TermId = termId,
                    TermTranslation = termElement.Element ("TermTranslation")?.Value,
                    TermVersionId = int.Parse (termElement.Attribute ("TermVersionId")?.Value ?? "0"),
                    WholeWord = termElement.Attribute ("WholeWord")?.Value
                };
                terms.Add (term);
            }

            var groupedTerms = terms.ToDictionary (term => term.TermId, term => term);

            var resultObjects = polidoc.Root.Element ("Result").Elements ("Object");
            var occurences = new List<PolicheckTermOccurrence> ();
            foreach (var resultObject in resultObjects) {
                var result = new PolicheckTermOccurrence {
                    Column = int.Parse (resultObject.Element ("Context").Attribute ("TermAt").Value),
                    Context = resultObject.Element ("Context").Value,
                    IssueStatus = resultObject.Element ("IssueStatus").Value,
                    Position = resultObject.Element ("Position").Value,
                    Url = resultObject.Attribute ("URL").Value,
                    TermId = int.Parse (resultObject.Element ("TermId").Value)
                };
                occurences.Add (result);
            }

            foreach (var grouping in occurences.GroupBy (o => o.TermId).Where (g => groupedTerms.ContainsKey (g.Key))) {
                groupedTerms [grouping.Key].Occurrences = grouping.ToArray ();
            }

            try {
                using (var writer = new StreamWriter (OutputPath, false))
                    new PolicheckHtmlTemplate () {
                        Model = groupedTerms.Values,
                    }.Generate (writer);


                var message = $"Found {groupedTerms.Count} new Policheck terms for review. " +
                    $"Please check the output in {OutputPath}.";

                if (groupedTerms.Count > 0) {
                    if (FailOnNonZeroTerms)
                        Log.LogError (message);
                    else
                        Log.LogWarning (message);
                }

                return groupedTerms.Count <= 0 || !FailOnNonZeroTerms;
            } catch (Exception e) {
                Log.LogError ($"Could not generate Policheck HTML.");
                Log.LogError (e.ToString());
                return false;
            }
        }
    }

    class PolicheckAction
    {
        public string Context { get; set; }
        public string Recommendation { get; set; }
    }

    class PolicheckTermOccurrence
    {
        public string Url { get; set; }
        public string Position { get; set; }
        public int Line => int.Parse (Position.Substring (5).Trim ());
        public string Context { get; set; }
        public int Column { get; set; }
        public string IssueStatus { get; set; }
        public int TermId { get; set; }
    }

    class PolicheckTerm
    {
        public int Severity { get; set; }
        public int TermId { get; set; }
        public string [] FieldsImpacted { get; set; }
        public int TermVersionId { get; set; }
        public int RowCount { get; set; }
        public string CommentSearch { get; set; }
        public string WholeWord { get; set; }
        public string CaseSensitive { get; set; }
        public string TermClass { get; set; }
        public string TermDefinition { get; set; }
        public string TermTranslation { get; set; }
        public string Comment { get; set; }
        public PolicheckAction [] Actions { get; set; }
        public PolicheckTermOccurrence [] Occurrences { get; set; }
    }
}
