//
// WorkbookTitledNode.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.ComponentModel;

using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Structure
{
	sealed class WorkbookTitledNode : FileNode
	{
		readonly IWorkbookTitledNode node;

		public WorkbookTitledNode (IWorkbookTitledNode node)
		{
			if (node == null)
				throw new ArgumentNullException (nameof (node));

			this.node = node;

			node.PropertyChanged += Node_PropertyChanged;
		}

		void Node_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (WorkbookPage.Title))
				NotifyPropertyChanged (nameof (Name));
		}

		public override object RepresentedObject {
			get { return node; }
			set {
				throw new InvalidOperationException ();
			}
		}

		public override string Name {
			get { return node.Title; }
			set {
				if (node.Title != value) {
					node.Title = value;
					NotifyPropertyChanged ();
				}
			}
		}
	}
}