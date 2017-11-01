//
// CompletionModel.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Xamarin.Interactive.CodeAnalysis
{
	sealed class CompletionModel
	{
		public SourceText Text { get; }

		public IList<CompletionItem> TotalItems { get; }
		public IList<CompletionItem> FilteredItems { get; }

		public CompletionItem SelectedItem { get; }
		public int SelectedItemFilteredIndex { get; }

		public int CommitTrackingSpanEnd { get; }

		CompletionModel (
			SourceText text,
			IList<CompletionItem> totalItems,
			IList<CompletionItem> filteredItems,
			CompletionItem selectedItem,
			int selectedItemFilteredIndex,
			int commitSpanEnd)
		{
			Text = text;
			TotalItems = totalItems;
			FilteredItems = filteredItems;
			SelectedItem = selectedItem;
			SelectedItemFilteredIndex = selectedItemFilteredIndex;
			CommitTrackingSpanEnd = commitSpanEnd;
		}

		public static CompletionModel CreateModel (
			SourceText text,
			TextSpan defaultTrackingSpanInSubjectBuffer,
			IList<CompletionItem> totalItems)
		{
			return new CompletionModel (
				text,
				totalItems,
				totalItems,
				totalItems.First (),
				0,
				defaultTrackingSpanInSubjectBuffer.End);
		}

		public CompletionModel WithFilteredItems (IList<CompletionItem> filteredItems)
		{
			return new CompletionModel (
				Text,
				TotalItems,
				filteredItems,
				filteredItems.First (),
				0,
				CommitTrackingSpanEnd);
		}

		public CompletionModel WithSelectedItem (
			CompletionItem selectedItem,
			int selectedItemFilteredIndex)
		{
			return (selectedItem == SelectedItem && selectedItemFilteredIndex == SelectedItemFilteredIndex)
				? this
				: new CompletionModel (
					Text,
					TotalItems,
					FilteredItems,
					selectedItem,
					selectedItemFilteredIndex,
					CommitTrackingSpanEnd);
		}

		public CompletionModel WithCommitTrackingSpanEnd (int commitSpanEnd)
		{
			return new CompletionModel (
				Text,
				TotalItems,
				FilteredItems,
				SelectedItem,
				SelectedItemFilteredIndex,
				commitSpanEnd);
		}

		public CompletionModel WithText (SourceText text)
		{
			return new CompletionModel (
				text,
				TotalItems,
				FilteredItems,
				SelectedItem,
				SelectedItemFilteredIndex,
				CommitTrackingSpanEnd);
		}
	}
}