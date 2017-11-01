//
// XIWebDocumentView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System.Collections.Generic;

using AppKit;
using Foundation;
using ObjCRuntime;
using WebKit;

namespace Xamarin.Interactive.Client.Mac.WebDocument
{
	[Register]
	[Adopts ("WebDocumentView")]
	abstract class XIWebDocumentView : NSView
	{
		readonly Dictionary<NSView, NSLayoutConstraint[]> ownedConstraints
			= new Dictionary<NSView, NSLayoutConstraint[]> ();

		protected abstract NSView ContentView { get; }

		protected WebDataSource DataSource { get; private set; }

		protected virtual void DataSourceUpdated (WebDataSource oldDataSource, WebDataSource newDataSource)
		{
		}

		public override void ViewDidMoveToSuperview ()
		{
			if (ContentView == null)
				return;

			if (ContentView.Superview == this)
				return;

			if (ContentView.Superview != null)
				ContentView.RemoveFromSuperview ();

			AddSubview (ContentView);

			ConstrainSubviewToSuperview (this, Superview);
			ConstrainSubviewToSuperview (ContentView, this);
		}

		void ConstrainSubviewToSuperview (NSView subview, NSView superview)
		{
			subview.TranslatesAutoresizingMaskIntoConstraints = false;

			NSLayoutConstraint[] constraints;

			if (ownedConstraints.TryGetValue (superview, out constraints))
				superview.RemoveConstraints (constraints);

			ownedConstraints.Add (superview, constraints = new [] {
				NSLayoutConstraint.Create (
					subview, NSLayoutAttribute.Width,
					NSLayoutRelation.Equal,
					superview, NSLayoutAttribute.Width,
					1,
					0),

				NSLayoutConstraint.Create (
					subview, NSLayoutAttribute.Height,
					NSLayoutRelation.Equal,
					superview, NSLayoutAttribute.Height,
					1,
					0)
			});

			superview.AddConstraints (constraints);
		}

		[Export ("setDataSource:")]
		void SetDataSource (WebDataSource dataSource)
		{
			DataSourceUpdated (dataSource);
		}

		[Export ("dataSourceUpdated:")]
		void DataSourceUpdated (WebDataSource dataSource)
		{
			var oldDataSource = DataSource;
			DataSource = dataSource;
			DataSourceUpdated (oldDataSource, DataSource);
		}

		[Export ("setNeedsLayout:")]
		void SetNeedsLayout (bool flag)
		{
			NeedsLayout = flag;
		}

		[Export ("layout")]
		new void Layout ()
		{
			base.Layout ();
		}

		[Export ("viewWillMoveToHostWindow:")]
		void ViewWillMoveToHostWindow (NSWindow hostWindow)
		{
			ViewWillMoveToWindow (hostWindow);
		}

		[Export ("viewDidMoveToHostWindow")]
		void ViewDidMoveToHostWindow ()
		{
			ViewDidMoveToWindow ();
		}
	}
}