using System;

using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	sealed partial class PackageAddWindow : NSWindow
	{
		public PackageAddWindow (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public PackageAddWindow (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
