// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Forms;

namespace Xamarin.Workbooks.Forms
{
	public partial class MainPage : ContentPage
	{
		public MainPage (params Page [] extraPages)
		{
			InitializeComponent ();
			PushButton.Clicked += async (sender, e) => await Navigation.PushAsync (new BoxViewClockPage ());
			foreach (var page in extraPages) {
				var button = new Button {
					Text = $"Show {page.GetType ().Name}",
				};
				button.Clicked += async (sender, e) => await Navigation.PushAsync (page);
				MainLayout.Children.Add (button);
			}
		}
	}
}
