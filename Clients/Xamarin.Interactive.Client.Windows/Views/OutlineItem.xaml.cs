// OutlineItem.xaml.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Client.Windows.Views
{
	public partial class OutlineItem : UserControl
	{
		public OutlineItem ()
		{
			InitializeComponent ();
		}

		public static readonly DependencyProperty ItemProperty =
			DependencyProperty.Register (
				nameof (Item),
				typeof (TreeNode),
				typeof (OutlineItem),
				new PropertyMetadata (null));

		internal TreeNode Item {
			get { return (TreeNode)GetValue (ItemProperty); }
			set { SetValue (ItemProperty, value); }
		}

		void OnKeyDown (object sender, KeyEventArgs args)
		{
			switch (args.Key) {
				case Key.Enter:
					var box = sender as TextBox;
					box?.GetBindingExpression (TextBox.TextProperty)?.UpdateSource ();
					Item.IsEditing = false;
					break;
				case Key.Escape:
					Item.IsEditing = false;
					break;
				default:
					break;
			}
		}

		void OnRename (object sender, RoutedEventArgs e)
		{
			if (Item.IsRenamable) {
				e.Handled = true;
				Item.IsEditing = true;
			}
		}

		void OnEditorLoaded (object sender, RoutedEventArgs e)
		{
			var box = sender as TextBox;
			box.Focus ();
			box.SelectAll ();
		}

		void OnDisplayLoaded (object sender, RoutedEventArgs e) =>
			Item.IsEditing = false;

		void OnLostFocus (object sender, RoutedEventArgs e) =>
			Item.IsEditing = false;

	}
}