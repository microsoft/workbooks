//
// MultiSourceImage.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Xamarin.Interactive.Client.Windows.Controls
{
	class MultiSourceImage : Image
	{
		public static readonly DependencyProperty SourcesProperty = DependencyProperty.Register (
			nameof (Sources),
			typeof (IReadOnlyList<ImageSource>),
			typeof (MultiSourceImage),
			new PropertyMetadata (OnPropertyChanged));

		public IReadOnlyList<ImageSource> Sources {
			get => (IReadOnlyList<ImageSource>)GetValue (SourcesProperty);
			set => SetValue (SourcesProperty, value);
		}

		void SetSource (IReadOnlyList<ImageSource> images)
		{
			if (images == null || images.Count == 0) {
				Source = null;
				return;
			}

			var scaleFactor = PresentationSource.FromVisual (this).CompositionTarget.TransformToDevice.M11;
			var minSize = Math.Max (Width, Height) * scaleFactor;

			Source = images.FirstOrDefault (i => i.Width >= minSize && i.Height >= minSize)
					?? images.LastOrDefault ();
		}

		static void OnPropertyChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var image = (MultiSourceImage)d;
			image.SetSource (image.Sources);
		}
	}
}