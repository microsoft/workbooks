//
// DamagedDownloadException.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Client.Updater
{
	sealed class DamagedDownloadException : Exception
	{
		public DamagedDownloadException (string message) : base (message)
		{
		}
	}
}