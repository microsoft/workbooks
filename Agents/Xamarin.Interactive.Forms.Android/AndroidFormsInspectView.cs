//
// FormsInspectView.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;

using Android.App;
using Android.Graphics;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using Xamarin.Interactive.Android;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Remote;

using XIVR = Xamarin.Interactive.Android.ViewRenderer;
using static Xamarin.Interactive.Forms.FormsInspectViewHelper;

namespace Xamarin.Interactive.Forms.Android
{
	[Serializable]
	class AndroidFormsRootInspectView : InspectView
	{
		public AndroidFormsRootInspectView ()
		{
			SetHandle (IntPtr.Zero);
		}

		protected AndroidFormsRootInspectView (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}

		protected override void UpdateCapturedImage ()
		{
			// TODO
		}
	}

	[Serializable]
	class AndroidFormsInspectView : InspectView
	{
		const string TAG = nameof (AndroidFormsInspectView);
		readonly Element element;

		static TypeMap<Func<Element, string>> supplementaryDescriptionMap = new TypeMap<Func<Element, string>> ();

		static AndroidFormsInspectView ()
		{
			supplementaryDescriptionMap.Add (typeof (NativeViewWrapper), true, (arg) => {
				return ((NativeViewWrapper)arg).NativeView.GetType ().Name;
			});
		}

		protected AndroidFormsInspectView (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}

		public new AndroidFormsInspectView Parent {
			get { return (AndroidFormsInspectView)base.Parent; }
			set { base.Parent = value; }
		}

		public new AndroidFormsInspectView Root {
			get { return (AndroidFormsInspectView)base.Root; }
		}

		public AndroidFormsInspectView (Exception ex)
		{
			DisplayName = ex == null
				? "No Xamarin.Forms hierarchy available"
				: "Error while getting hierarchy";
		}

		public AndroidFormsInspectView (Page container, Page page, bool withSubviews = true)
		{
			if (container == null)
				throw new ArgumentNullException (nameof (container));

			if (page == null)
				throw new ArgumentNullException (nameof (page));

			element = container;

			PopulateTypeInformationFromObject (container);

			X = container.Bounds.X;
			Y = container.Bounds.Y;
			Width = container.Bounds.Width;
			Height = container.Bounds.Height;
			Kind = ViewKind.Primary;
			Visibility = container.GetViewVisibility ();

			if (withSubviews)
				HandleContainerChildren (
					container,
					page,
					p => new AndroidFormsInspectView (p, true),
					e => new AndroidFormsInspectView (e),
					AddSubview
				);
		}

		public AndroidFormsInspectView (Page page, bool useNativeViewBounds = false, bool withSubviews = true)
		{
			if (page == null)
				throw new ArgumentNullException (nameof (page));

			element = page;

			PopulateTypeInformationFromObject (page);

			// TODO: Pull the ClassId or some user-set property as the description?
			var nativeView = Platform.GetRenderer (page).ViewGroup;
			if (!useNativeViewBounds) {
				Transform = XIVR.GetViewTransform (nativeView);
				if (Transform == null) {
					X = page.Bounds.X;
					Y = page.Bounds.Y;
				}
			} else {
				var location = new int [2];
				nativeView.GetLocationOnScreen (location);

				X = location [0];
				// You are the worst, Android. Here we need to convert the location we get to dips,
				// because Xamarin.Forms Bounds are in dips, and then also subtract the status bar,
				// once again converting to dips.
				var yDips = nativeView.Context.FromPixels (location [1]);

				var window = ((Activity)nativeView.Context).Window;
				var rect = new Rect ();
				window.DecorView.GetWindowVisibleDisplayFrame (rect);
				var statusBarHeight = rect.Top;
				var statusBarDips = nativeView.Context.FromPixels (statusBarHeight);

				Y = yDips - statusBarDips;
			}

			Width = page.Bounds.Width;
			Height = page.Bounds.Height;
			Kind = ViewKind.Primary;
			Visibility = page.GetViewVisibility ();

			if (withSubviews)
				HandlePageChildren (
					page,
					(p, b) => new AndroidFormsInspectView (p, useNativeViewBounds || b),
					e => new AndroidFormsInspectView (e),
					AddSubview
				);
		}

		public AndroidFormsInspectView (Element element, bool withSubviews = true)
		{
			if (element == null)
				throw new ArgumentNullException (nameof (element));

			this.element = element;

			PopulateTypeInformationFromObject (element);

			var velement = element as VisualElement;
			if (velement != null) {
				var nativeView = Platform.GetRenderer (velement).ViewGroup;

				DisplayName = element.GetType ().Name;
				try {
					DisplayName += " :" + nativeView.Resources.GetResourceName (nativeView.Id).TrimId ();
				} catch {
				}

				Transform = XIVR.GetViewTransform (nativeView);
				if (Transform == null) {
					X = velement.Bounds.X;
					Y = velement.Bounds.Y;
				}
				Width = velement.Bounds.Width;
				Height = velement.Bounds.Height;
				Visibility = velement.GetViewVisibility ();
			} else {
				// Since this is not a visual element, set it as collapsed by default.
				Visibility = ViewVisibility.Collapsed;
			}

			Kind = ViewKind.Primary;

			// TODO: Figure out different types of elements and extra useful data from them when appropriate.
			Description = GetDescriptionFromElement (element, supplementaryDescriptionMap);

			if (withSubviews)
				HandleElementChildren (element, e => new AndroidFormsInspectView (e), AddSubview);
		}

		protected override void UpdateCapturedImage ()
		{
			VisualElement ve;
			if ((ve = element as VisualElement) != null) {
				// If the VisualElement is a view and is not a layout, snapshot its children,
				// as we've reached the leaf of the tree. Otherwise, skip children.
				var skipChildren = !(ve is View && !(ve is Layout));
				var nativeView = Platform.GetRenderer (ve).ViewGroup;
				if (nativeView != null)
					CapturedImage = XIVR.RenderAsPng (nativeView, skipChildren);
			}
		}
	}
}