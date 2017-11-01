//
// IStatusMessageViewDelegate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Messages
{
	interface IStatusMessageViewDelegate
	{
		bool CanDisplayMessage (Message message);
		void StartSpinner ();
		void StopSpinner ();
		void DisplayMessage (Message message);
		void DisplayIdle ();
	}
}