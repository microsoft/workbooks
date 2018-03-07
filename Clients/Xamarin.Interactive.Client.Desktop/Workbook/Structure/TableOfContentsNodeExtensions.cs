//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Xamarin.Interactive.Workbook.Structure
{
    static class TableOfContentsNodeExtensions
    {
        public static void RebuildFromJavaScript (this TableOfContentsNode node, dynamic headings)
        {
            var headingsCount = headings.length;
            if (headingsCount <= 0) {
                node.Children.Clear ();
                return;
            }

            // Build a complete list of the new headings, reusing nodes from
            // the existing headings model where possible (same old IDs).
            // This will ensure the "Become" call does as little work as
            // possible, simply diffing the two models and applying relevant
            // changes. In turn this allows the views to react nicely (e.g.
            // by preserving selections and expansion state).

            var updatedList = new List<TableOfContentsNode> (headingsCount);

            for (int i = 0; i < headingsCount; i++) {
                var heading = headings [i];
                var oldId = heading.oldId;

                var childNode = node.Children.FirstOrDefault (n => n.Id == oldId)
                    ?? new TableOfContentsNode (false);
                childNode.Id = heading.newId;
                childNode.Name = heading.text;

                updatedList.Add (childNode);
            }

            node.Children.UpdateTo (updatedList);
        }
    }
}