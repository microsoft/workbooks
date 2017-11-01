//
// ListViewPage.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Xamarin.Workbooks.Forms
{
	public class ListViewPage : ContentPage
	{
		public ListViewPage ()
		{
			var tableItems = new List<string> ();
			for (var i = 0; i < 100; i++) {
				tableItems.Add (Guid.NewGuid ().ToString ("n"));
			}

			var listView = new ListView ();
			listView.ItemsSource = tableItems;
			listView.ItemTemplate = new DataTemplate (typeof (TextCell));
			listView.ItemTemplate.SetBinding (TextCell.TextProperty, ".");

			listView.ItemSelected += async (sender, e) => {
				if (e.SelectedItem == null)
					return;
				listView.SelectedItem = null; // deselect row
				await DisplayAlert ("Selected Item", $"Selected item is {listView.SelectedItem}.", "OK");
			};

			double topThickness;
			switch (Device.RuntimePlatform) {
			case Device.iOS:
				topThickness = 20;
				break;
			default:
				topThickness = 0;
				break;
			}

			Content = new StackLayout {
				Padding = new Thickness (5, topThickness, 5, 0),
				Children = {
					new Label {
						HorizontalTextAlignment = TextAlignment.Center,
						Text = "Xamarin.Forms built-in ListView"
					},
					listView
				}
			};
		}
	}
}
