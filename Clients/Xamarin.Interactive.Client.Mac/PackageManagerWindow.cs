using System;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	sealed partial class PackageManagerWindow : NSWindow
	{
		public PackageManagerWindow (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public PackageManagerWindow (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
